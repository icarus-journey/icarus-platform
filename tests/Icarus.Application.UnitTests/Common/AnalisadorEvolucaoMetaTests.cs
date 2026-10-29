using Icarus.Application.Common;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.UnitTests.Common;

public sealed class AnalisadorEvolucaoMetaTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 25, 1, 0, 0, TimeSpan.Zero);
    private static Campanha Meta() => new() { ValorAtual = 8, ValorMensurado = 16, Unidade = "módulos", Estado = EstadoCampanha.EmAndamento };
    private static MedicaoCampanha[] Historico() => Enumerable.Range(0, 5).Select(i => new MedicaoCampanha
    {
        Id = i + 1,
        ValorAnterior = i == 0 ? null : 2 * (i - 1),
        ValorAtual = 2 * i,
        ValorMensurado = 16,
        Unidade = "módulos",
        RegistradaEm = Agora.AddDays(-28 + 7 * i),
        FusoHorario = "America/Sao_Paulo"
    }).ToArray();

    [Fact]
    public void ConectaHistoricoRealAoCalculadorEUsaDataLocalSemInventarBaseline()
    {
        var resultado = AnalisadorEvolucaoMeta.Analisar(Meta(), Historico().Reverse().ToArray(), Agora);
        Assert.Equal(8m, resultado.Evolucao.AvancoRegistrado);
        Assert.Equal(8m, resultado.Evolucao.Faltante);
        Assert.Equal(new DateOnly(2026, 9, 26), resultado.Evolucao.Pontos[0].Data);
        Assert.Equal(new DateOnly(2026, 10, 24), resultado.Evolucao.Pontos[^1].Data);
        Assert.Equal(new CenarioConquista(true, "cenario_condicional_se_mantiver_o_ritmo", 2m, 8m, 4), resultado.Cenario);
        var unica = AnalisadorEvolucaoMeta.Analisar(Meta(), [Historico()[^1]], Agora);
        Assert.Null(unica.Evolucao.AvancoRegistrado);
        Assert.Single(unica.Evolucao.Pontos);
        Assert.False(unica.Cenario.Disponivel);
    }

    [Fact]
    public void UltimoRegistroDoDiaApareceNoGraficoMasDuplicataNaoLiberaCenario()
    {
        var historico = Historico().Append(new MedicaoCampanha
        {
            Id = 6,
            ValorAnterior = 8,
            ValorAtual = 7,
            ValorMensurado = 16,
            Unidade = "módulos",
            RegistradaEm = Agora.AddMinutes(1),
            FusoHorario = "America/Sao_Paulo"
        }).ToArray();
        var meta = Meta(); meta.ValorAtual = 7;
        var resultado = AnalisadorEvolucaoMeta.Analisar(meta, historico, Agora.AddHours(1));
        Assert.Equal(5, resultado.Evolucao.Pontos.Count);
        Assert.Equal(7m, resultado.Evolucao.Pontos[^1].Valor);
        Assert.True(resultado.Evolucao.Pontos[^1].Revisado);
        Assert.False(resultado.Cenario.Disponivel);
        Assert.Equal("datas_duplicadas", resultado.Cenario.Motivo);
    }

    [Theory]
    [InlineData("ponto")]
    [InlineData(" pontos ")]
    [InlineData("XP")]
    public void GamificacaoNaoViraEvolucaoDeResultado(string unidade)
    {
        var meta = Meta(); meta.Unidade = unidade;
        var resultado = AnalisadorEvolucaoMeta.Analisar(meta, Historico(), Agora);
        Assert.Empty(resultado.Evolucao.Pontos);
        Assert.Null(resultado.Evolucao.Faltante);
        Assert.False(resultado.Cenario.Disponivel);
    }

    [Fact]
    public void TrocaDeAlvoOuUnidadeInterrompeTrechoSemMisturarMedidas()
    {
        foreach (var unidade in new[] { "módulos", "reais" })
        {
            var historico = Historico();
            historico[2] = new MedicaoCampanha
            {
                Id = 3,
                ValorAtual = 100,
                ValorMensurado = 20,
                Unidade = unidade,
                RegistradaEm = historico[2].RegistradaEm,
                FusoHorario = "America/Sao_Paulo"
            };
            var resultado = AnalisadorEvolucaoMeta.Analisar(Meta(), historico, Agora);
            Assert.Equal(2, resultado.Evolucao.Pontos.Count);
            Assert.Equal(2m, resultado.Evolucao.AvancoRegistrado);
            Assert.False(resultado.Cenario.Disponivel);
        }
        var meta = Meta(); meta.Unidade = "reais";
        var alterada = AnalisadorEvolucaoMeta.Analisar(meta, Historico(), Agora);
        Assert.Empty(alterada.Evolucao.Pontos);
        Assert.Equal("meta_alterada", alterada.Cenario.Motivo);
    }

    [Fact]
    public void ValorEditadoSemMedicaoNaoUsaRitmoAntigo()
    {
        var meta = Meta(); meta.ValorAtual = 10;
        var resultado = AnalisadorEvolucaoMeta.Analisar(meta, Historico(), Agora);
        Assert.Equal(6m, resultado.Evolucao.Faltante);
        Assert.Null(resultado.Evolucao.AvancoRegistrado);
        Assert.Equal("aguardando_novo_registro", resultado.Cenario.Motivo);
    }

    [Fact]
    public void SemMetaSemFusoOuSemHistoricoRetornaAusenciaExplicita()
    {
        Assert.Equal("historico_insuficiente", AnalisadorEvolucaoMeta.Analisar(Meta(), [], Agora).Cenario.Motivo);
        var meta = Meta(); meta.ValorMensurado = null;
        Assert.Equal("meta_nao_definida", AnalisadorEvolucaoMeta.Analisar(meta, [], Agora).Cenario.Motivo);
        var semFuso = Historico().Select(m => new MedicaoCampanha
        {
            ValorAtual = m.ValorAtual,
            ValorMensurado = m.ValorMensurado,
            Unidade = m.Unidade,
            RegistradaEm = m.RegistradaEm,
            FusoHorario = "inexistente"
        }).ToArray();
        Assert.Equal("fuso_nao_comparavel", AnalisadorEvolucaoMeta.Analisar(Meta(), semFuso, Agora).Cenario.Motivo);
    }

    [Fact]
    public void NovoTrechoNoMesmoFusoPodeRetomarCenarioSemDescartarRevisoesRecentes()
    {
        var antigo = new MedicaoCampanha
        {
            Id = 0,
            ValorAtual = 0,
            ValorMensurado = 16,
            Unidade = "módulos",
            RegistradaEm = Agora.AddDays(-35),
            FusoHorario = "UTC"
        };
        var resultado = AnalisadorEvolucaoMeta.Analisar(Meta(), Historico().Prepend(antigo).ToArray(), Agora);
        Assert.Equal(5, resultado.Evolucao.Pontos.Count);
        Assert.True(resultado.Cenario.Disponivel);
        var comReducao = Historico();
        comReducao[4] = new MedicaoCampanha
        {
            ValorAnterior = 9,
            ValorAtual = 8,
            ValorMensurado = 16,
            Unidade = "módulos",
            RegistradaEm = Agora,
            FusoHorario = "America/Sao_Paulo"
        };
        Assert.Equal("medicoes_nao_comparaveis", AnalisadorEvolucaoMeta.Analisar(Meta(), comReducao, Agora).Cenario.Motivo);
    }

    [Theory]
    [InlineData(EstadoCampanha.Concluida)]
    [InlineData(EstadoCampanha.Excluida)]
    public void CampanhaEncerradaMantemEvolucaoSemProjetar(EstadoCampanha estado)
    {
        var meta = Meta(); meta.Estado = estado;
        var resultado = AnalisadorEvolucaoMeta.Analisar(meta, Historico(), Agora);
        Assert.Equal(5, resultado.Evolucao.Pontos.Count);
        Assert.Equal("campanha_encerrada", resultado.Cenario.Motivo);
    }

    [Fact]
    public void MetaAlcancadaHistoricoAntigoEFuturoNaoApresentamPrazo()
    {
        var meta = Meta(); meta.ValorMensurado = 8;
        var historico = Historico().Select(m => new MedicaoCampanha
        {
            ValorAtual = m.ValorAtual,
            ValorMensurado = 8,
            Unidade = m.Unidade,
            RegistradaEm = m.RegistradaEm,
            FusoHorario = m.FusoHorario
        }).ToArray();
        Assert.Equal("meta_alcancada", AnalisadorEvolucaoMeta.Analisar(meta, historico, Agora).Cenario.Motivo);
        Assert.False(AnalisadorEvolucaoMeta.Analisar(Meta(), Historico(), Agora.AddDays(8)).Cenario.Disponivel);
        Assert.False(AnalisadorEvolucaoMeta.Analisar(Meta(), Historico(), Agora.AddMinutes(-1)).Cenario.Disponivel);
    }
}
