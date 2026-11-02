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
    {
        var usuario = await contexto.Usuarios.AsNoTracking().Where(u => u.Id == usuarioId)
            .Select(u => new { u.CriadoEm, u.PlanejamentoVersionadoDesde }).SingleOrDefaultAsync(ct);
        var versoes = await contexto.Set<RegistroPlanejamentoOcorrencia>().AsNoTracking()
            .Where(v => v.Ocorrencia.Missao.UsuarioId == usuarioId)
            .Select(v => new VersaoPlanejamento(v.Id, v.OcorrenciaId, v.Ocorrencia.MissaoId, v.DataPrevista, v.Ativa, v.RegistradoEm)).ToListAsync(ct);
        var execucoes = await contexto.Ocorrencias.AsNoTracking().Where(o => o.Missao.UsuarioId == usuarioId)
            .Select(o => new ExecucaoPlanejada(o.Id, o.MissaoId, o.DataPrevista, o.Status, o.RealizadaEm)).ToListAsync(ct);
        var completo = usuario is not null && (usuario.PlanejamentoVersionadoDesde <= usuario.CriadoEm
            || usuario.PlanejamentoVersionadoDesde <= periodo.InicioUtc);
        var versionadas = versoes.Select(v => v.OcorrenciaId).ToHashSet();
        if (execucoes.Any(e => e.DataPrevista <= periodo.Fim && !versionadas.Contains(e.Id))) completo = false;
        var lacuna = false;
        if (completo)
        {
            var recorrentes = await contexto.Missoes.AsNoTracking().Include(m => m.Recorrencia).Include(m => m.Campanha)
                .Where(m => m.UsuarioId == usuarioId && m.Recorrencia != null && m.CriadoEm < periodo.FimExclusivoUtc).ToListAsync(ct);
            var existentes = versoes.Where(v => v.RegistradoEm < periodo.FimExclusivoUtc).Select(v => (v.MissaoId, v.DataPrevista)).ToHashSet();
            lacuna = recorrentes.Any(m => TemLacuna(m, periodo, existentes))
                || await contexto.Missoes.AsNoTracking().AnyAsync(m => m.UsuarioId == usuarioId && m.Recorrencia == null
                    && !m.Ocorrencias.Any() && m.CriadoEm < periodo.FimExclusivoUtc
                    && m.DataLimite >= periodo.Inicio && m.DataLimite <= periodo.Fim
                    && (m.Status == StatusMissao.Ativa || m.PlanejamentoEncerradoEm >= periodo.InicioUtc), ct);
        }
        var resumo = CalculadorAdesao.Calcular(periodo, versoes, execucoes, completo, lacuna);
        var registros = resumo.Estado == "historico_parcial"
            ? execucoes.Where(e => e.DataPrevista >= periodo.Inicio && e.DataPrevista <= periodo.Fim && e.Status != StatusOcorrencia.Excluida).Select(e => e.Id).ToHashSet()
            : CalculadorAdesao.Vigentes(periodo, versoes).Select(v => v.OcorrenciaId).ToHashSet();
        return (resumo, registros);
    }

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
