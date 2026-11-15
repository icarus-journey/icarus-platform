using Icarus.Application.Common;

namespace Icarus.Application.UnitTests.Common;

public sealed class CalculadorCenarioConquistaTests
{
    private static MedicaoConquista[] Historico() => Enumerable.Range(0, 5)
        .Select(i => new MedicaoConquista(new DateOnly(2026, 9, 21).AddDays(7 * i), 2 * i, 16, "módulos")).ToArray();

    [Fact]
    public void DeveProjetarSomenteResultadoMensuradoComQuatroSemanasComparaveis()
    {
        var cenario = CalculadorCenarioConquista.Calcular(Historico(), new DateOnly(2026, 10, 19));
        Assert.True(cenario.Disponivel);
        Assert.Equal(2m, cenario.RitmoSemanal);
        Assert.Equal(8m, cenario.Faltante);
        Assert.Equal(4, cenario.SemanasAproximadas);
    }

    [Theory]
    [InlineData("pontos")]
    [InlineData("XP")]
    [InlineData("")]
    public void PontosNaoDevemVirarConquistaProjetada(string unidade)
    {
        var historico = Historico().Select(m => m with { Unidade = unidade });
        Assert.False(CalculadorCenarioConquista.Calcular(historico, new DateOnly(2026, 10, 19)).Disponivel);
    }

    [Fact]
    public void CorrecaoMudancaDeAlvoOuReducaoDevemImpedirProjecao()
    {
        foreach (var modificada in new[] { Historico()[2] with { Correcao = true }, Historico()[2] with { Alvo = 20 }, Historico()[2] with { Valor = 1 } })
        {
            var historico = Historico();
            historico[2] = modificada;
            Assert.False(CalculadorCenarioConquista.Calcular(historico, new DateOnly(2026, 10, 19)).Disponivel);
        }
    }

    [Fact]
    public void AusenciaDeHistoricoOuMudancaDePeriodicidadeNaoDeveGerarPromessa()
    {
        Assert.False(CalculadorCenarioConquista.Calcular(Historico().Take(4), new DateOnly(2026, 10, 19)).Disponivel);
        var historico = Historico();
        historico[2] = historico[2] with { Data = historico[2].Data.AddDays(1) };
        Assert.False(CalculadorCenarioConquista.Calcular(historico, new DateOnly(2026, 10, 19)).Disponivel);
    }

    [Theory]
    [InlineData(35, 7, "antes_do_prazo", 1.6)]
    [InlineData(28, 0, "no_prazo", 2d)]
    [InlineData(21, -7, "depois_do_prazo", 2.67)]
    [InlineData(0, -28, "depois_do_prazo", null)]
    [InlineData(-1, -29, "depois_do_prazo", null)]
    public void RelacionaPrazoSemInferirProbabilidade(int dias, int margem, string situacao, double? necessario)
    {
        var hoje = new DateOnly(2026, 11, 8);
        var resultado = CalculadorCenarioConquista.RelacionarPrazo(new(true, "condicional", 2, 8, 4), hoje, hoje.AddDays(dias));
        Assert.Equal(new DateOnly(2026, 12, 6), resultado.Prazo!.ConclusaoEstimada);
        Assert.Equal(margem, resultado.Prazo.MargemDias);
        Assert.Equal(situacao, resultado.Prazo.Situacao);
        Assert.Equal((decimal?)necessario, resultado.Prazo.RitmoSemanalNecessario);
    }
    [Fact]
    public void ProjecaoInvalidaNaoGanhaPrazoENaoEstouraCalendario()
    {
        var hoje = DateOnly.MaxValue;
        Assert.Null(CalculadorCenarioConquista.RelacionarPrazo(new(false, "historico_insuficiente"), hoje, hoje).Prazo);
        Assert.False(CalculadorCenarioConquista.RelacionarPrazo(new(true, "condicional", 1, 1, int.MaxValue), hoje, hoje).Disponivel);
    }
}
