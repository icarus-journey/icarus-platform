using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;

namespace Icarus.Application.UnitTests.Analise;

public class ComparacaoSemanalTests
{
    private static SemanaPlanejada Semana(int indice, int previstas = 10, int feitas = 8)
    {
        var inicio = new DateOnly(2026, 10, 12).AddDays(-7 * indice);
        return new(new(inicio, inicio.AddDays(6), "America/Sao_Paulo"), previstas == 0 ? "sem_planejamento" : "disponivel",
            previstas, feitas, previstas == 0 ? null : decimal.Round(100m * feitas / previstas, 1));
    }
    private static SemanaPlanejada[] Anteriores() => [Semana(1, 8, 4), Semana(2, 4, 2), Semana(3, 12, 9), Semana(4, 10, 5)];

    [Fact]
    public void ComparaAsTresMedidasEMediaAritmeticaDeQuatroSemanasSemPonderarPeloVolume()
    {
        var r = CalculadorComparacaoSemanal.Calcular(Semana(0), Anteriores());
        Assert.True(r.PodeCompararSemanaAnterior); Assert.True(r.PodeCompararMediaQuatroSemanas);
        Assert.False(r.HistoricoInsuficiente);
        Assert.Equal(new MediaPlanejada(8.5m, 5m, 56.2m), r.MediaQuatroSemanas);
    }

    [Fact]
    public void SemanaParcialNaoViraZeroENaoBloqueiaComparacaoAnteriorCompleta()
    {
        var semanas = Anteriores();
        semanas[2] = semanas[2] with { Estado = "historico_parcial", Previstas = null, Concluidas = null, TaxaConclusao = null };
        var r = CalculadorComparacaoSemanal.Calcular(Semana(0), semanas);
        Assert.True(r.PodeCompararSemanaAnterior); Assert.False(r.PodeCompararMediaQuatroSemanas);
        Assert.True(r.HistoricoInsuficiente); Assert.Equal(new MediaPlanejada(null, null, null), r.MediaQuatroSemanas);
    }

    [Fact]
    public void SemanaSemPlanejamentoNaoEntraComoTaxaZeroNemEhDescartadaDaMedia()
    {
        var semanas = Anteriores(); semanas[3] = Semana(4, 0, 0);
        var r = CalculadorComparacaoSemanal.Calcular(Semana(0), semanas);
        Assert.Null(r.MediaQuatroSemanas.TaxaConclusao); Assert.Null(r.MediaQuatroSemanas.Previstas);
        Assert.False(CalculadorComparacaoSemanal.Calcular(Semana(0, 0, 0), Anteriores()).PodeCompararSemanaAnterior);
    }

    [Fact]
    public void TaxaZeroComPlanejamentoConhecidoEhComparavel()
    {
        var semanas = Enumerable.Range(1, 4).Select(i => Semana(i, 10, 0)).ToArray();
        var r = CalculadorComparacaoSemanal.Calcular(Semana(0, 10, 0), semanas);
        Assert.True(r.PodeCompararMediaQuatroSemanas); Assert.Equal(0m, r.MediaQuatroSemanas.TaxaConclusao);
    }

    [Fact]
    public void RejeitaSemanasFaltantesDuplicadasOuDeOutroFuso()
    {
        Assert.Throws<ArgumentException>(() => CalculadorComparacaoSemanal.Calcular(Semana(0), Anteriores()[..3]));
        var s = Anteriores(); s[2] = s[1];
        Assert.Throws<ArgumentException>(() => CalculadorComparacaoSemanal.Calcular(Semana(0), s));
        s = Anteriores(); s[2] = s[2] with { Periodo = s[2].Periodo with { FusoHorario = "UTC" } };
        Assert.Throws<ArgumentException>(() => CalculadorComparacaoSemanal.Calcular(Semana(0), s));
    }

    [Fact]
    public void ListaPermitidaNuncaConfundeConclusoesDeAtividadeComConcluidasDoPlanejamento()
    {
        var dados = System.Text.Json.JsonSerializer.Deserialize<Icarus.Application.Common.Analise.IndicadoresPessoais>(
            "{\"concluidas\":12,\"distribuicaoPorArea\":[],\"limitacoes\":[]}", ContratoRelatorioSemanal.Json)!;
        var permitidos = IndicadoresRelatorioSemanal.DosIndicadores(dados);
        Assert.Equal(12, permitidos.Concluidas); Assert.Null(permitidos.Previstas); Assert.Null(permitidos.TaxaConclusao);
    }
}
