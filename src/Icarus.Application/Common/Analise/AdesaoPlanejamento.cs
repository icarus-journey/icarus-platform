using Icarus.Domain.Enums;

namespace Icarus.Application.Common.Analise;

public sealed record VersaoPlanejamento(long Id, long OcorrenciaId, long MissaoId, DateOnly DataPrevista,
    bool Ativa, DateTimeOffset RegistradoEm);
public sealed record ExecucaoPlanejada(long Id, long MissaoId, DateOnly DataPrevista, StatusOcorrencia Status,
    DateTimeOffset? RealizadaEm);
public sealed record ResumoAdesao(string Estado, int PrevistasRegistradas, int? ConcluidasDoPlanejamento,
    int? Pendentes, int? Atrasadas, decimal? TaxaConclusao, string? Motivo);

public static class CalculadorAdesao
{
    public static ResumoAdesao Calcular(PeriodoAnalitico periodo, IReadOnlyList<VersaoPlanejamento> versoes,
        IReadOnlyList<ExecucaoPlanejada> execucoes, bool historicoCompleto, bool lacunaRecorrente)
    {
        var corte = periodo.FimExclusivoUtc;
        var vigentes = Vigentes(periodo, versoes);
        var porId = execucoes.ToDictionary(e => e.Id);
        var incompletas = vigentes.Any(v => !porId.TryGetValue(v.OcorrenciaId, out var e)
            || (e.Status is StatusOcorrencia.Concluida or StatusOcorrencia.ConcluidaComAtraso && e.RealizadaEm is null));
        if (!historicoCompleto || lacunaRecorrente || incompletas)
        {
            var observadas = execucoes.Count(e => e.DataPrevista >= periodo.Inicio && e.DataPrevista <= periodo.Fim && e.Status != StatusOcorrencia.Excluida);
            return new("historico_parcial", observadas, null, null, null, null,
                lacunaRecorrente ? "janela_incompleta" : incompletas ? "realizacao_sem_data" : "planejamento_legado");
        }
        var concluidas = vigentes.Count(v => porId[v.OcorrenciaId] is var e
            && e.Status is StatusOcorrencia.Concluida or StatusOcorrencia.ConcluidaComAtraso && e.RealizadaEm < corte);
        // Os filtros públicos são períodos encerrados; todo planejamento não realizado já venceu.
        var abertas = vigentes.Length - concluidas;
        return new(vigentes.Length == 0 ? "sem_planejamento" : "disponivel", vigentes.Length,
            concluidas, 0, abertas, vigentes.Length == 0 ? null : decimal.Round(100m * concluidas / vigentes.Length, 1), null);
    }
    public static VersaoPlanejamento[] Vigentes(PeriodoAnalitico periodo, IReadOnlyList<VersaoPlanejamento> versoes)
        => versoes.Where(v => v.RegistradoEm < periodo.FimExclusivoUtc).GroupBy(v => v.OcorrenciaId)
            .Select(g => g.OrderByDescending(v => v.RegistradoEm).ThenByDescending(v => v.Id).First())
            .Where(v => v.Ativa && v.DataPrevista >= periodo.Inicio && v.DataPrevista <= periodo.Fim).ToArray();
}
