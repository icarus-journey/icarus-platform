using Icarus.Application.Common.Analise;
using Icarus.Domain.Enums;

namespace Icarus.Application.UnitTests.Analise;

public sealed class CalculadorAdesaoTests
{
    private static readonly PeriodoAnalitico Periodo = new(new(2027, 1, 4), new(2027, 1, 10), "UTC");
    private static DateTimeOffset Instante(int dia, int hora = 12) => new(2027, 1, dia, hora, 0, 0, TimeSpan.Zero);
    private static VersaoPlanejamento Versao(long id, long ocorrencia, int dia, int registrada = 1, bool ativa = true)
        => new(id, ocorrencia, ocorrencia, new(2027, 1, dia), ativa, Instante(registrada));
    private static ExecucaoPlanejada Execucao(long id, int dia, int? feita = null)
        => new(id, id, new(2027, 1, dia), feita is null ? StatusOcorrencia.Pendente : StatusOcorrencia.Concluida,
            feita is { } d ? Instante(d) : null);

    [Fact]
    public void OitoDeDezPlanejadasTemOitentaPorCentoEDuasAtrasadas()
    {
        var versoes = Enumerable.Range(1, 10).Select(i => Versao(i, i, 6)).ToArray();
        var execucoes = Enumerable.Range(1, 10).Select(i => Execucao(i, 6, i <= 8 ? 6 : null)).ToArray();
        var r = CalculadorAdesao.Calcular(Periodo, versoes, execucoes, true, false);
        Assert.Equal("disponivel", r.Estado); Assert.Equal(10, r.PrevistasRegistradas);
        Assert.Equal(8, r.ConcluidasDoPlanejamento); Assert.Equal(80, r.TaxaConclusao);
        Assert.Equal(0, r.Pendentes); Assert.Equal(2, r.Atrasadas);
    }

    [Theory]
    [InlineData(10, 100)]
    [InlineData(11, 0)]
    [InlineData(12, 0)]
    public void RealizacaoDepoisDoFechamentoNaoMudaATaxaAnterior(int dia, int taxa)
    {
        var r = CalculadorAdesao.Calcular(Periodo, [Versao(1, 1, 6)], [Execucao(1, 6, dia)], true, false);
        Assert.Equal(taxa, r.TaxaConclusao);
    }

    [Fact]
    public void AtividadeDeOutroPlanejamentoNaoEntraNoNumerador()
    {
        var r = CalculadorAdesao.Calcular(Periodo, [Versao(1, 1, 6), Versao(2, 2, 3)],
            [Execucao(1, 6), Execucao(2, 3, 6)], true, false);
        Assert.Equal(1, r.PrevistasRegistradas); Assert.Equal(0, r.ConcluidasDoPlanejamento);
        Assert.Equal(0, r.TaxaConclusao);
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(12, 1)]
    public void CancelamentoSoAlteraOPlanejamentoSeFoiAntesDoFechamento(int registrado, int previstas)
    {
        var r = CalculadorAdesao.Calcular(Periodo, [Versao(1, 1, 6), Versao(2, 1, 6, registrado, false)],
            [new(1, 1, new(2027, 1, 6), StatusOcorrencia.Excluida, null)], true, false);
        Assert.Equal(previstas, r.PrevistasRegistradas);
    }

    [Fact]
    public void ReagendarDepoisDoFechamentoPreservaADataAnterior()
    {
        var r = CalculadorAdesao.Calcular(Periodo, [Versao(1, 1, 6), Versao(2, 1, 15, 12)],
            [Execucao(1, 15)], true, false);
        Assert.Equal(1, r.PrevistasRegistradas); Assert.Equal(1, r.Atrasadas);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void LacunaOuLegadoMostraObservadasSemEstimarTaxa(bool completo, bool lacuna)
    {
        var r = CalculadorAdesao.Calcular(Periodo, [Versao(1, 1, 6)], [Execucao(1, 6, 6)], completo, lacuna);
        Assert.Equal("historico_parcial", r.Estado); Assert.Equal(1, r.PrevistasRegistradas);
        Assert.Null(r.TaxaConclusao); Assert.Null(r.ConcluidasDoPlanejamento);
    }

    [Fact]
    public void SemPlanejamentoNaoCriaZeroPorCento()
    {
        var r = CalculadorAdesao.Calcular(Periodo, [], [], true, false);
        Assert.Equal("sem_planejamento", r.Estado); Assert.Null(r.TaxaConclusao);
    }

    [Fact]
    public void ConclusaoSemRealizacaoConfirmadaNaoAutorizaTaxa()
    {
        var r = CalculadorAdesao.Calcular(Periodo, [Versao(1, 1, 6)],
            [new(1, 1, new(2027, 1, 6), StatusOcorrencia.Concluida, null)], true, false);
        Assert.Equal("realizacao_sem_data", r.Motivo); Assert.Null(r.TaxaConclusao);
    }
}
