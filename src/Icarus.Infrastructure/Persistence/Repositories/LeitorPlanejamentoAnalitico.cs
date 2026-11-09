using Icarus.Application.Common;
using Icarus.Application.Common.Analise;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

internal static class LeitorPlanejamentoAnalitico
{
    internal static async Task<(ResumoAdesao Resumo, HashSet<long> Registros)> LerAsync(
        IcarusDbContext contexto, Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct)
        => (await LerPeriodosAsync(contexto, usuarioId, [periodo], ct))[0];

    internal static async Task<IReadOnlyList<(ResumoAdesao Resumo, HashSet<long> Registros)>> LerPeriodosAsync(
        IcarusDbContext contexto, Guid usuarioId, IReadOnlyList<PeriodoAnalitico> periodos, CancellationToken ct)
    {
        if (periodos.Count == 0) return [];
        var inicio = periodos.Min(p => p.Inicio);
        var fim = periodos.Max(p => p.Fim);
        var corte = periodos.Max(p => p.FimExclusivoUtc);
        var usuario = await contexto.Usuarios.AsNoTracking().Where(u => u.Id == usuarioId)
            .Select(u => new { u.CriadoEm, u.PlanejamentoVersionadoDesde }).SingleOrDefaultAsync(ct);
        var historico = contexto.Set<RegistroPlanejamentoOcorrencia>().AsNoTracking();
        var candidatas = historico.Where(v => v.DataPrevista >= inicio && v.DataPrevista <= fim);
        // Uma ocorrência movida para fora da janela ainda precisa da última versão anterior ao corte.
        // A existência escalar favorece a busca pelo índice da ocorrência, sem materializar outros titulares.
        var versoes = await historico.Where(v => v.Ocorrencia.Missao.UsuarioId == usuarioId
                && v.RegistradoEm < corte
                && candidatas.Where(c => c.OcorrenciaId == v.OcorrenciaId).Select(c => (long?)c.Id).FirstOrDefault() != null)
            .Select(v => new VersaoPlanejamento(v.Id, v.OcorrenciaId, v.Ocorrencia.MissaoId, v.DataPrevista, v.Ativa, v.RegistradoEm))
            .ToListAsync(ct);
        var execucoesDoUsuario = contexto.Ocorrencias.AsNoTracking().Where(o => o.Missao.UsuarioId == usuarioId);
        var execucoes = await execucoesDoUsuario.Where(o => (o.DataPrevista >= inicio && o.DataPrevista <= fim)
                || candidatas.Where(c => c.OcorrenciaId == o.Id).Select(c => (long?)c.Id).FirstOrDefault() != null)
            .Select(o => new ExecucaoPlanejada(o.Id, o.MissaoId, o.DataPrevista, o.Status, o.RealizadaEm)).ToListAsync(ct);
        // Legado anterior à janela continua impedindo afirmar que o planejamento é completo.
        var primeiroLegado = await execucoesDoUsuario.Where(o => o.DataPrevista <= fim
                && historico.Where(v => v.OcorrenciaId == o.Id).Select(v => (long?)v.Id).FirstOrDefault() == null)
            .Select(o => (DateOnly?)o.DataPrevista).MinAsync(ct);
        bool Completo(PeriodoAnalitico periodo) => usuario is not null
            && (usuario.PlanejamentoVersionadoDesde <= usuario.CriadoEm || usuario.PlanejamentoVersionadoDesde <= periodo.InicioUtc)
            && !(primeiroLegado <= periodo.Fim);

        var resultados = new List<(ResumoAdesao Resumo, HashSet<long> Registros)>();
        var recorrentes = new List<Missao>();
        var avulsasSemOcorrencia = new List<AvulsaSemOcorrencia>();
        if (periodos.Any(Completo))
        {
            recorrentes = await contexto.Missoes.AsNoTracking().Include(m => m.Recorrencia).Include(m => m.Campanha)
                .Where(m => m.UsuarioId == usuarioId && m.Recorrencia != null && m.CriadoEm < corte).ToListAsync(ct);
            avulsasSemOcorrencia = await contexto.Missoes.AsNoTracking()
                .Where(m => m.UsuarioId == usuarioId && m.Recorrencia == null && !m.Ocorrencias.Any()
                    && m.CriadoEm < corte && m.DataLimite >= inicio && m.DataLimite <= fim)
                .Select(m => new AvulsaSemOcorrencia(m.DataLimite, m.CriadoEm, m.Status, m.PlanejamentoEncerradoEm)).ToListAsync(ct);
        }
        foreach (var periodo in periodos)
        {
            var completo = Completo(periodo);
            var existentes = versoes.Where(v => v.RegistradoEm < periodo.FimExclusivoUtc)
                .Select(v => (v.MissaoId, v.DataPrevista)).ToHashSet();
            var lacuna = completo && (recorrentes.Any(m => m.CriadoEm < periodo.FimExclusivoUtc && TemLacuna(m, periodo, existentes))
                || avulsasSemOcorrencia.Any(m => m.CriadoEm < periodo.FimExclusivoUtc
                    && m.DataLimite >= periodo.Inicio && m.DataLimite <= periodo.Fim
                    && (m.Status == StatusMissao.Ativa || m.PlanejamentoEncerradoEm >= periodo.InicioUtc)));
            var resumo = CalculadorAdesao.Calcular(periodo, versoes, execucoes, completo, lacuna);
            var registros = resumo.Estado == "historico_parcial"
                ? execucoes.Where(e => e.DataPrevista >= periodo.Inicio && e.DataPrevista <= periodo.Fim && e.Status != StatusOcorrencia.Excluida).Select(e => e.Id).ToHashSet()
                : CalculadorAdesao.Vigentes(periodo, versoes).Select(v => v.OcorrenciaId).ToHashSet();
            resultados.Add((resumo, registros));
        }
        return resultados;
    }

    private sealed record AvulsaSemOcorrencia(DateOnly? DataLimite, DateTimeOffset CriadoEm,
        StatusMissao Status, DateTimeOffset? PlanejamentoEncerradoEm);

    private static bool TemLacuna(Missao missao, PeriodoAnalitico periodo, HashSet<(long MissaoId, DateOnly DataPrevista)> existentes)
    {
        if (missao.Status != StatusMissao.Ativa && missao.PlanejamentoEncerradoEm is null) return false;
        var inicio = RegrasRecorrencia.Inicio(missao);
        if (inicio < periodo.Inicio) inicio = periodo.Inicio;
        var fim = periodo.Fim;
        if (missao.Campanha?.PrazoFim is { } prazo && prazo < fim) fim = prazo;
        if (missao.PlanejamentoEncerradoEm is { } encerrada)
        {
            var dia = FusoHorario.DataLocal(encerrada, periodo.FusoHorario);
            if (dia < fim) fim = dia;
        }
        try
        {
            for (var data = inicio; data <= fim; data = data.AddDays(1))
            {
                var versao = PlanejamentoOcorrencia.DaExecucao(missao, new() { DataPrevista = data });
                var limite = versao is null ? missao.Recorrencia!.DataFim : versao.DataFimRecorrencia;
                if (limite < data) continue;
                var tipo = Enum.Parse<TipoRecorrencia>(versao?.TipoRecorrencia ?? missao.Recorrencia!.Tipo, true);
                var regra = versao?.Regra ?? missao.Recorrencia!.Regra;
                if (GeradorDeDatasRecorrencia.Gerar(data, data, tipo, regra).Count > 0 && !existentes.Contains((missao.Id, data))) return true;
            }
            return false;
        }
        catch (Exception erro) when (erro is System.Text.Json.JsonException or KeyNotFoundException or ArgumentException or InvalidOperationException)
        { return true; } // Regra legada ilegível não autoriza uma porcentagem.
    }
}
