using Icarus.Application.Common.Analise;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.UnitTests.Analise;

public sealed class CalculadorPanoramaObjetivosTests
{
    private static readonly DateTimeOffset Agora = new(2027, 1, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly PeriodoAnalitico Periodo = new(new(2027, 1, 4), new(2027, 1, 10), "UTC");
    private static Campanha Campanha(long id, string titulo = "Meta", string unidade = "módulos") => new()
    { Id = id, EpicoId = 1, Titulo = titulo, Estado = EstadoCampanha.EmAndamento, ValorMensurado = 12, ValorAtual = 8, Unidade = unidade };
    private static Epico Epico(params Campanha[] campanhas) => new() { Id = 1, Titulo = "Estudar", Campanhas = campanhas.ToList() };
    private static AtividadeObjetivo Feita(long campanha, int dia, int hora = 12) => new(campanha, 1,
        new(2027, 1, dia, hora, 0, 0, TimeSpan.Zero), new(2027, 1, dia, hora, 1, 0, TimeSpan.Zero));
    private static MedicaoCampanha Medir(long id, int dia, decimal valor, string unidade = "módulos", decimal alvo = 12) => new()
    { Id = id, CampanhaId = 1, ValorAtual = valor, ValorMensurado = alvo, Unidade = unidade, RegistradaEm = new(2027, 1, dia, 12, 0, 0, TimeSpan.Zero), FusoHorario = "UTC" };

    [Fact]
    public void SeparaAtividadeDoPeriodoDeDestaqueRecenteEValoresAtuais()
    {
        var c = Campanha(1); var outro = Campanha(2, "Alfa");
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(c, outro)],
            [Feita(1, 6), Feita(1, 7), Feita(2, 19)], [Medir(1, 3, 2), Medir(2, 6, 4), Medir(3, 10, 6), Medir(4, 19, 8)], Periodo, Agora);
        Assert.Equal(2, r.CampanhaEmDestaqueId); Assert.Equal(2, r.CampanhasEmAndamento);
        var meta = r.Campanhas.Single(x => x.Id == 1);
        Assert.Equal(2, meta.Atividade.ConcluidasNoPeriodo); Assert.Equal(0, meta.Atividade.ConcluidasNosUltimosSeteDias);
        Assert.Equal(8, meta.Resultado.ValorAtual); Assert.Equal(4, meta.Resultado.EvolucaoNoPeriodo.AvancoRegistrado);
        Assert.Equal("registro_anterior", meta.Resultado.BaseDoAvanco); Assert.Equal(2, meta.Resultado.RegistrosNoPeriodo);
        Assert.Equal(2, meta.Resultado.EvolucaoNoPeriodo.Pontos.Count);
    }

    [Fact]
    public void EmpateUsaTituloAlfabeticoENaoPrioridadeManualOuArea()
    {
        var z = Campanha(1, "Zulu"); z.AreaDaVida = AreaDaVida.EstudosEAprendizado;
        var a = Campanha(2, "Alfa");
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(z, a)], [Feita(1, 19), Feita(2, 19)], [], Periodo, Agora);
        Assert.Equal(2, r.CampanhaEmDestaqueId);
    }

    [Fact]
    public void SemConclusaoRecenteNaoDerivaDestaqueEPlanejadaOuConcluidaNaoDisputa()
    {
        var a = Campanha(1); var b = Campanha(2); b.Estado = EstadoCampanha.Planejada;
        var c = Campanha(3); c.Estado = EstadoCampanha.Concluida;
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(a, b, c)], [Feita(1, 6), Feita(2, 19), Feita(3, 19)], [], Periodo, Agora);
        Assert.Null(r.CampanhaEmDestaqueId); Assert.Equal(1, r.CampanhasSemRegistroRecente);
        Assert.Equal(1, r.Epicos[0].Campanhas.Planejadas); Assert.Equal(1, r.Epicos[0].Campanhas.Concluidas);
    }

    [Fact]
    public void DatasIncertasNaoAfirmamInatividadeNemDestaqueExato()
    {
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(Campanha(1), Campanha(2))],
            [Feita(1, 19), new(2, 1, null, Agora)], [], Periodo, Agora);
        Assert.Equal("historico_incompleto", r.EstadoDestaque); Assert.Null(r.CampanhasSemRegistroRecente);
        Assert.Null(r.CampanhaEmDestaqueId); Assert.Null(r.Campanhas.Single(c => c.Id == 2).Atividade.ConcluidasNosUltimosSeteDias);
    }

    [Fact]
    public void ContagensPorEstadoNaoViraramPercentualEExcluidasNaoViraramLinks()
    {
        var a = Campanha(1); var b = Campanha(2); b.Estado = EstadoCampanha.Excluida;
        var oculto = new Epico { Id = 3, Estado = EstadoEpico.Excluido, Campanhas = [Campanha(4)] };
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(a, b), oculto], [Feita(2, 6)], [], Periodo, Agora);
        Assert.Single(r.Campanhas); Assert.Single(r.Epicos); Assert.Equal(1, r.Epicos[0].Atividade.ConcluidasNoPeriodo);
        Assert.Equal(1, r.Epicos[0].Campanhas.EmAndamento);
        Assert.DoesNotContain("percent", System.Text.Json.JsonSerializer.Serialize(r.Epicos), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("pontos")]
    [InlineData("XP")]
    [InlineData(" ponto ")]
    public void PontosNaoViraramResultadoMensuradoOuCenario(string unidade)
    {
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(Campanha(1, unidade: unidade))], [], [Medir(1, 6, 20, unidade)], Periodo, Agora);
        var meta = r.Campanhas[0].Resultado;
        Assert.Equal("unidade_sem_resultado_mensurado", meta.Estado); Assert.Null(meta.ValorAtual);
        Assert.Null(meta.EvolucaoNoPeriodo.AvancoRegistrado); Assert.False(meta.CenarioAtual.Disponivel);
    }

    [Fact]
    public void NaoSomaMedidasDiferentesNemInventaAvancoComUmRegistroIsolado()
    {
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(Campanha(1))], [], [Medir(1, 6, 2, "reais"), Medir(2, 9, 8)], Periodo, Agora);
        var meta = r.Campanhas[0].Resultado;
        Assert.Null(meta.EvolucaoNoPeriodo.AvancoRegistrado); Assert.Single(meta.EvolucaoNoPeriodo.Pontos);
        Assert.Equal(2, meta.RegistrosNoPeriodo); Assert.Null(meta.BaseDoAvanco);
    }

    [Fact]
    public void ReducaoPreservaValorNegativoEValorAtualAcimaDoAlvoNaoETruncado()
    {
        var c = Campanha(1); c.ValorAtual = 14;
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(c)], [], [Medir(1, 6, 6), Medir(2, 9, 4)], Periodo, Agora);
        Assert.Equal(-2, r.Campanhas[0].Resultado.EvolucaoNoPeriodo.AvancoRegistrado);
        Assert.Equal(14, r.Campanhas[0].Resultado.ValorAtual);
    }

    [Fact]
    public void JanelaRecenteIncluiHojeRespeitaFusoEIgnoraFuturo()
    {
        var fuso = Periodo with { FusoHorario = "America/Sao_Paulo" };
        var r = CalculadorPanoramaObjetivos.Calcular([Epico(Campanha(1))],
            [Feita(1, 14, 2), Feita(1, 14, 4), Feita(1, 20, 10), Feita(1, 21)], [], fuso, Agora);
        Assert.Equal(2, r.Campanhas[0].Atividade.ConcluidasNosUltimosSeteDias);
        Assert.Equal(new DateOnly(2027, 1, 14), r.JanelaRecente.Inicio);
    }

    [Fact]
    public void SemObjetivosRetornaConjuntosVaziosEValoresAtuaisZeroConfirmados()
    {
        var r = CalculadorPanoramaObjetivos.Calcular([], [], [], Periodo, Agora);
        Assert.Empty(r.Campanhas); Assert.Empty(r.Epicos); Assert.Equal(0, r.CampanhasEmAndamento);
        Assert.Equal(0, r.CampanhasSemRegistroRecente);
    }
}
