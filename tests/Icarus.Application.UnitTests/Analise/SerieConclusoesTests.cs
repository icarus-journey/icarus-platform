using Icarus.Application.Common.Analise;

namespace Icarus.Application.UnitTests.Analise;

public sealed class SerieConclusoesTests
{
    [Fact]
    public void SemanaContaConclusoesDoMesmoDiaEMantemDiasSemRegistro()
    {
        var periodo = new PeriodoAnalitico(new(2026, 10, 12), new(2026, 10, 18), "America/Sao_Paulo");
        var serie = SerieConclusoes.DasDatas(periodo, [new(2026, 10, 12), new(2026, 10, 12), new(2026, 10, 18)])!;
        Assert.Equal("dia", serie.Granularidade);
        Assert.Equal(7, serie.Pontos.Count);
        Assert.Equal([2, 0, 0, 0, 0, 0, 1], serie.Pontos.Select(p => p.Concluidas));
    }

    [Fact]
    public void MesTemBlocosDeSeteDiasEOUltimoNaoUltrapassaOPeriodo()
    {
        var periodo = new PeriodoAnalitico(new(2026, 7, 1), new(2026, 7, 31), "UTC");
        var serie = SerieConclusoes.DasDatas(periodo, [new(2026, 7, 7), new(2026, 7, 8), new(2026, 7, 31)])!;
        Assert.Equal("semana", serie.Granularidade);
        Assert.Equal(5, serie.Pontos.Count);
        Assert.Equal(new DateOnly(2026, 7, 29), serie.Pontos[^1].Inicio);
        Assert.Equal(periodo.Fim, serie.Pontos[^1].Fim);
        Assert.Equal(3, serie.Pontos.Sum(p => p.Concluidas));
    }

    [Fact]
    public void SeisMesesAtravessamAnoPreservandoOrdemEMesesSemConclusoes()
    {
        var periodo = new PeriodoAnalitico(new(2025, 10, 1), new(2026, 3, 31), "UTC");
        var serie = SerieConclusoes.DasDatas(periodo, [new(2026, 3, 31), new(2025, 10, 12)])!;
        Assert.Equal("mes", serie.Granularidade);
        Assert.Equal(6, serie.Pontos.Count);
        Assert.Equal([1, 0, 0, 0, 0, 1], serie.Pontos.Select(p => p.Concluidas));
        Assert.Equal(new DateOnly(2026, 2, 28), serie.Pontos[4].Fim);
    }

    [Fact]
    public void SemConclusoesNaoInventaSerie()
        => Assert.Null(SerieConclusoes.DasDatas(new(new(2026, 10, 12), new(2026, 10, 18), "UTC"), []));
}
