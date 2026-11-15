using Icarus.Application.Common;
using Icarus.Application.Common.Jornada;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

/// <summary>Regras de fechamento executadas somente sob a transação e o bloqueio do titular.</summary>
public sealed class ProtecaoSequenciaRepository(IcarusDbContext contexto, TimeProvider relogio) : IProtecaoSequencia
{
    public void SolicitarRecalculo(Guid usuarioId) => contexto.Set<OutboxJornada>().Add(new()
    {
        UsuarioId = usuarioId,
        CriadaEm = relogio.GetUtcNow()
    });

    public async Task ReconciliarAsync(Usuario usuario, CancellationToken ct)
    {
        if (contexto.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Fechamento exige transação e bloqueio do usuário.");
        if (!FusoHorario.EhValido(usuario.FusoHorario)) return;
        var agora = relogio.GetUtcNow();
        usuario.FusoInicioProtecao ??= usuario.FusoHorario;
        var inicio = FusoHorario.DataLocal(usuario.ProtecaoAtivadaEm, usuario.FusoInicioProtecao);
        var hoje = FusoHorario.DataLocal(agora, usuario.FusoHorario);
        if (inicio > hoje) return;
        var registros = await LeitorRealizacoes.LerAsync(contexto, usuario.Id, ct);
        var apuracao = new ApuracaoRealizacoes(registros, agora);
        var dias = await contexto.Set<FechamentoJornada>().Where(x => x.UsuarioId == usuario.Id).ToDictionaryAsync(x => x.Data, ct);
        var resgates = await contexto.Set<ResgateItem>().Where(x => x.UsuarioId == usuario.Id && x.Protecao).ToListAsync(ct);
        var consumos = await contexto.Set<ConsumoProtecao>().Include(x => x.Resgate).Include(x => x.Devolucao)
            .Where(x => x.UsuarioId == usuario.Id).ToListAsync(ct);
        var consumosPorId = consumos.ToDictionary(x => x.Id);
        var origens = new Dictionary<ResgateItem, ResgateItem>();
        ResgateItem Origem(ResgateItem unidade)
        {
            if (origens.TryGetValue(unidade, out var origem)) return origem;
            origem = unidade.CompensacaoOrigemId is { } chave
                ? Origem(consumosPorId[chave].Resgate) : unidade;
            origens.Add(unidade, origem);
            return origem;
        }
        var grupos = resgates.GroupBy(Origem).ToArray();
        // Cada compra corresponde a um único direito de uso, inclusive após devoluções.
        // Reconstituir esses direitos evita depender da ordem em que mensagens chegaram.
        var direitos = grupos.Where(g => !g.Any(r => r.Estado == EstadoResgate.Estornado)
                && (g.Any(r => r.Estado == EstadoResgate.Resgatado)
                    || consumos.Any(c => Origem(c.Resgate) == g.Key && (c.CompensadoEm is null || c.Devolucao is null))))
            .Select(g => g.Key).OrderBy(r => r.ResgatadoEm).ThenBy(r => r.Id).ToList();
        var alocados = new HashSet<ResgateItem>();
        var desejados = new Dictionary<DateOnly, ResgateItem>();
        var decisoes = new List<(DateOnly Data, string Fuso, string Estado)>();
        bool? continuidade = inicio == DateOnly.MinValue ? false : apuracao.NoDia(inicio.AddDays(-1));
        var ultima = dias.Count == 0 ? hoje.AddDays(-1) : new[] { hoje.AddDays(-1), dias.Keys.Max() }.Max();
        for (var data = inicio; data <= ultima; data = data.AddDays(1))
        {
            var fuso = dias.GetValueOrDefault(data)?.FusoHorario ?? usuario.FusoHorario!;
            var fim = InicioDia(data.AddDays(1), fuso);
            if (fim > agora) break;
            var realizado = apuracao.NoDia(data);
            var elegivel = realizado == false && continuidade == true && InicioDia(data, fuso) < fim;
            var direito = elegivel ? direitos.FirstOrDefault(r => !alocados.Contains(r) && r.ResgatadoEm < fim) : null;
            if (direito is not null) { desejados.Add(data, direito); alocados.Add(direito); }
            var estado = realizado == true ? "realizado" : realizado is null ? "indeterminado"
                : direito is not null ? "protegido" : "interrompido";
            decisoes.Add((data, fuso, estado));
            continuidade = realizado == true || direito is not null ? true : realizado;
        }

        foreach (var consumo in consumos.Where(c => c.CompensadoEm is null))
            if (!desejados.TryGetValue(consumo.Data, out var direito) || direito != Origem(consumo.Resgate))
                consumo.CompensadoEm = agora;
        // Libera a unicidade do consumo vigente antes de inserir sua substituição.
        // O commit continua único: falha em qualquer etapa reverte também esta gravação.
        await contexto.SaveChangesAsync(ct);

        ResgateItem Devolver(ConsumoProtecao consumo)
        {
            var origem = consumo.Resgate;
            var unidade = new ResgateItem
            {
                UsuarioId = usuario.Id,
                ItemId = origem.ItemId,
                OperacaoId = Guid.NewGuid(),
                Titulo = origem.Titulo,
                Descricao = origem.Descricao,
                Custo = origem.Custo,
                Protecao = true,
                ResgatadoEm = agora,
                CompensacaoOrigemId = consumo.Id,
                DisponivelProtecaoDesde = Origem(origem).ResgatadoEm
            };
            contexto.Add(unidade);
            consumo.Devolucao = unidade;
            resgates.Add(unidade);
            origens.Add(unidade, Origem(origem));
            return unidade;
        }
        foreach (var decisao in decisoes)
        {
            if (desejados.TryGetValue(decisao.Data, out var direito)
                && !consumos.Any(c => c.Data == decisao.Data && c.CompensadoEm is null))
            {
                var unidade = resgates.SingleOrDefault(r => Origem(r) == direito && r.Estado == EstadoResgate.Resgatado);
                if (unidade is null)
                {
                    var pendente = consumos.Single(c => Origem(c.Resgate) == direito && c.CompensadoEm != null && c.Devolucao is null);
                    // Uma devolução pode ser usada diretamente no recálculo, sem guardar uma quarta unidade.
                    unidade = Devolver(pendente);
                }
                unidade.Estado = EstadoResgate.Utilizado;
                unidade.UtilizadoEm = agora;
                var consumo = new ConsumoProtecao
                {
                    UsuarioId = usuario.Id,
                    Data = decisao.Data,
                    FusoHorario = decisao.Fuso,
                    Resgate = unidade,
                    ConsumidoEm = agora
                };
                contexto.Add(consumo);
                consumos.Add(consumo);
                consumosPorId.Add(consumo.Id, consumo);
            }
            if (!dias.TryGetValue(decisao.Data, out var fechamento))
            {
                fechamento = new FechamentoJornada { UsuarioId = usuario.Id, Data = decisao.Data, FusoHorario = decisao.Fuso };
                contexto.Add(fechamento);
                dias.Add(decisao.Data, fechamento);
            }
            if (fechamento.Estado != decisao.Estado)
            {
                fechamento.Estado = decisao.Estado;
                fechamento.AtualizadoEm = agora;
            }
        }
        foreach (var pendente in consumos.Where(c => c.CompensadoEm != null && c.Devolucao is null)
            .OrderBy(c => c.CompensadoEm).ThenBy(c => c.Id))
        {
            if (resgates.Count(r => r.Estado == EstadoResgate.Resgatado) >= 3) break;
            Devolver(pendente);
        }
        await contexto.SaveChangesAsync(ct);
    }

    public static DateTimeOffset InicioDia(DateOnly data, string fuso)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(fuso);
        var local = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (zona.IsInvalidTime(local)) local = local.AddMinutes(1);
        var deslocamento = zona.IsAmbiguousTime(local) ? zona.GetAmbiguousTimeOffsets(local).Max() : zona.GetUtcOffset(local);
        return new DateTimeOffset(local, deslocamento).ToUniversalTime();
    }
}

internal static class LeitorRealizacoes
{
    internal static async Task<List<RealizacaoSequencia>> LerAsync(IcarusDbContext contexto, Guid usuarioId, CancellationToken ct)
    {
        var registros = await contexto.Ocorrencias.AsNoTracking()
            .Where(o => o.Missao.UsuarioId == usuarioId
                && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(o => new RealizacaoSequencia(o.RealizadaEm, o.FusoHorarioRealizacao, o.ConcluidaEm)).ToListAsync(ct);
        registros.AddRange(await contexto.Missoes.AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId && m.ConcluidaEm != null
                && !m.Ocorrencias.Any(o => o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(m => new RealizacaoSequencia(null, null, m.ConcluidaEm)).ToListAsync(ct));
        return registros;
    }
}
