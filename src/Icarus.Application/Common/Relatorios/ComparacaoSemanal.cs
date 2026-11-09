using Icarus.Application.Common.Analise;
using System.Text.Json.Serialization;

namespace Icarus.Application.Common.Relatorios;

public sealed record SemanaPlanejada(PeriodoAnalitico Periodo, string Estado, int? Previstas,
    int? Concluidas, decimal? TaxaConclusao);
public sealed record MediaPlanejada(decimal? Previstas, decimal? Concluidas, decimal? TaxaConclusao);
public sealed record ComparacaoSemanal(SemanaPlanejada Semana, IReadOnlyList<SemanaPlanejada> SemanasAnteriores,
    MediaPlanejada MediaQuatroSemanas, bool PodeCompararSemanaAnterior, bool PodeCompararMediaQuatroSemanas)
{
    [JsonIgnore]
    public bool HistoricoInsuficiente => !PodeCompararSemanaAnterior || !PodeCompararMediaQuatroSemanas;
}

public static class CalculadorComparacaoSemanal
{
    public static SemanaPlanejada DaAdesao(PeriodoAnalitico periodo, ResumoAdesao adesao)
        => new(periodo, adesao.Estado, adesao.Estado is "disponivel" or "sem_planejamento" ? adesao.PrevistasRegistradas : null,
            adesao.ConcluidasDoPlanejamento, adesao.TaxaConclusao);

    public static ComparacaoSemanal Calcular(SemanaPlanejada semana, IReadOnlyList<SemanaPlanejada> anteriores)
    {
        if (anteriores.Count != 4 || anteriores.Where((s, i) => s.Periodo != new PeriodoAnalitico(
            semana.Periodo.Inicio.AddDays(-7 * (i + 1)), semana.Periodo.Fim.AddDays(-7 * (i + 1)), semana.Periodo.FusoHorario)).Any())
            throw new ArgumentException("A comparação exige as quatro semanas anteriores consecutivas.", nameof(anteriores));
        var mediaDisponivel = anteriores.All(s => s.Estado == "disponivel" && s.TaxaConclusao is not null);
        // Média aritmética de quatro taxas semanais; semanas sem denominador não são descartadas nem viram zero.
        var media = mediaDisponivel ? new MediaPlanejada(anteriores.Average(s => (decimal)s.Previstas!.Value),
            anteriores.Average(s => (decimal)s.Concluidas!.Value), decimal.Round(anteriores.Average(s => s.TaxaConclusao!.Value), 1))
            : new MediaPlanejada(null, null, null);
        return new(semana, anteriores, media, semana.TaxaConclusao is not null && anteriores[0].TaxaConclusao is not null,
            semana.TaxaConclusao is not null && mediaDisponivel);
    }
}

public sealed record DadosRelatorioSemanal(IndicadoresPessoais Indicadores, ComparacaoSemanal ComparacaoSemanal,
    string VersaoContrato = "1.2.0");
