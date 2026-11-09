using Icarus.Application.Common.Analise;

namespace Icarus.Application.UnitTests.Analise;

public sealed class LeiturasQuantitativasTests
{
    private static SnapshotReferenciasPublicas Snapshot(params ReferenciaPublica[] referencias) => new("checksum",
        new("gold", "silver", "2.0.0", "2.0.0", "2.1.0", [
            new("atus_activity", "2024", "https://www.bls.gov/tus/", "hash-atus"),
            new("vigitel_harmonizado", "2024", "https://svs.aids.gov.br/daent/cgdnt/vigitel/", "hash-vigitel")]), referencias);

    private static ReferenciaPublica Sono(decimal? ativo = 29.1m, decimal? outro = 35.3m,
        string indicador = "vigitel_atividade_sintomas_sono", string desfecho = "sintomas_insonia") => new()
        {
            Id = indicador,
            Indicador = indicador,
            Fonte = "Vigitel",
            Edicao = 2024,
            Medida = "associacao",
            AreaDaVida = "SONO_E_RECUPERACAO",
            FaixaEtaria = "18–28",
            TipoDia = "todos",
            Valores = new()
            {
                Unidade = "percentual_de_pessoas_e_pontos_percentuais",
                GrupoExpostoPercentual = ativo,
                GrupoNaoExpostoPercentual = outro,
                DiferencaPp = -6.2m,
                Exposicao = "ativo_lazer",
                Desfecho = desfecho
            },
            Limitacoes = ["Associação bruta, sem ajuste ou intervalo de confiança."]
        };

    private static ReferenciaPublica Estudo(string id, decimal? percentual, bool? trabalha = null,
        bool? estuda = null, string faixa = "18–28", string dia = "todos", int edicao = 2024) => new()
        {
            Id = id,
            Indicador = "atus_aulas_tarefas_estudo",
            Fonte = "ATUS",
            Edicao = edicao,
            AreaDaVida = "ESTUDOS_E_APRENDIZADO",
            Medida = "participacao_no_dia",
            FaixaEtaria = faixa,
            TipoDia = dia,
            Filtros = new(estuda, trabalha),
            Populacao = "População civil não institucionalizada dos EUA",
            Denominador = "Todos os diários válidos",
            Valores = new() { Percentual = percentual, Unidade = "percentual_de_pessoas" }
        };

    private static IReadOnlyList<LeituraQuantitativa> Ler(SnapshotReferenciasPublicas snapshot,
        string area = "ESTUDOS_E_APRENDIZADO", int idade = 25, string dia = "todos") =>
        ConstrutorLeiturasQuantitativas.Construir(snapshot, area, idade, dia, new(1, "Campanha", 8, 16, "módulos"));

    [Fact]
    public void AsDuasRelacoesSonoUsamValoresPublicadosEConservamSuaLinhagem()
    {
        var snapshot = Snapshot(Sono(), Sono(68m, 74.1m, "vigitel_atividade_deseja_mais_sono", "deseja_mais_sono"));
        var leituras = Ler(snapshot, "CORPO_E_SAUDE");
        Assert.Equal(2, leituras.Count);
        Assert.Contains("18% menos relatos de sintomas de insônia", leituras[0].Leitura);
        Assert.Contains("8% menos relatos de vontade de dormir mais", leituras[1].Leitura);
        Assert.Equal([29.1m, 35.3m], leituras[0].Grupos.Select(g => g.Percentual));
        Assert.Equal([68m, 74.1m], leituras[1].Grupos.Select(g => g.Percentual));
        Assert.Equal("18–28", leituras[0].FaixaEtaria);
        Assert.Equal("hash-vigitel", Assert.Single(leituras[0].Origens).Sha256);
        Assert.Equal(snapshot.Referencias[0].Id, Assert.Single(leituras[0].ReferenciasIds));
        Assert.Contains("26 capitais", leituras[0].DadosDaPesquisa);
        Assert.Contains("sem ajuste", Assert.Single(leituras[0].Limitacoes));
    }

    [Theory]
    [InlineData(15, 20, "25% menos")]
    [InlineData(30, 20, "50% mais")]
    [InlineData(20, 20, "mesma proporção")]
    [InlineData(0, 0, "mesma proporção")]
    [InlineData(20, 0, "não cabe calcular")]
    public void NarrativaSegueODadoSemFixarBeneficioOuDividirPorZero(int ativo, int outro, string esperado)
    {
        var leitura = Assert.Single(Ler(Snapshot(Sono(ativo, outro)), "SONO_E_RECUPERACAO"));
        Assert.Contains(esperado, leitura.Leitura);
        if (ativo >= outro) Assert.DoesNotContain("menos relatos", leitura.Recomendacao.Texto);
        Assert.DoesNotContain("reduz", leitura.Leitura);
        Assert.DoesNotContain("chance", leitura.Leitura);
    }

    [Fact]
    public void ComparaOcupacaoSemSobreposicaoECalculaComplementoDoMesmoUniverso()
    {
        var leituras = Ler(Snapshot(Estudo("todos", 20.7m), Estudo("ocupados", 13.6m, true),
            Estudo("nao-ocupados", 42.1m, false), Estudo("estudam-ocupados", 43.8m, true, true)));
        Assert.Equal(2, leituras.Count);
        Assert.Contains("68% menos participação", leituras[0].Leitura);
        Assert.Equal(["ocupados", "nao-ocupados"], leituras[0].ReferenciasIds);
        Assert.Equal([20.7m, 79.3m], leituras[1].Grupos.Select(g => g.Percentual));
        Assert.Contains("1 em cada 5", leituras[1].Leitura);
        Assert.Contains("naquele dia", leituras[1].Leitura);
        Assert.Contains("Estados Unidos", leituras[0].DadosDaPesquisa);
        Assert.DoesNotContain("tempo integral", leituras[0].DadosDaPesquisa);
        Assert.Contains("não isola um efeito", leituras[0].DadosDaPesquisa);
    }

    [Fact]
    public void ParIncompletoNaoAceitaGrupoSemFiltroComoSeFosseNaoOcupado()
    {
        var leitura = Assert.Single(Ler(Snapshot(Estudo("todos", 20.7m), Estudo("ocupados", 13.6m, true))));
        Assert.EndsWith("participacao", leitura.Id);
        Assert.Equal("todos", Assert.Single(leitura.ReferenciasIds));
    }

    [Fact]
    public void ParAmpliaOsDoisGruposJuntosSemMisturarFaixaOuTipoDeDia()
    {
        var leitura = Assert.Single(Ler(Snapshot(Estudo("folha", 5, true, faixa: "25–28", dia: "segunda_sexta"),
            Estudo("amplo-a", 10, true), Estudo("amplo-b", 20, false)), dia: "segunda_sexta"));
        Assert.Equal(["amplo-a", "amplo-b"], leitura.ReferenciasIds);
        Assert.Equal("18–28", leitura.FaixaEtaria);
        Assert.Equal("todos", leitura.TipoDia);
    }

    [Fact]
    public void NaoComparaEdicoesDistintasNemGruposDeMatriculaSobrepostos()
    {
        Assert.Empty(Ler(Snapshot(Estudo("a", 10, true), Estudo("b", 20, false, edicao: 2023))));
        Assert.Empty(Ler(Snapshot(Estudo("a", 10, true, true), Estudo("b", 20, estuda: true))));
    }

    [Fact]
    public void AusenciaSupressaoEMenorDeIdadeNaoGeramZeroOuRecorteAdulto()
    {
        Assert.Empty(Ler(Snapshot(Sono()), "CORPO_E_SAUDE", idade: 17));
        Assert.Empty(Ler(Snapshot(Sono(null, 20)), "SONO_E_RECUPERACAO"));
        Assert.Empty(Ler(Snapshot(Sono(-1, 20)), "SONO_E_RECUPERACAO"));
        Assert.Empty(Ler(Snapshot(Estudo("a", 101))));
        Assert.Empty(Ler(Snapshot(Estudo("a", 20)), idade: 14));
        Assert.Empty(Ler(Snapshot(Estudo("a", 20)), "FINANCAS"));
        var semOrigem = Snapshot(Sono());
        Assert.Empty(Ler(semOrigem with { Metadados = semOrigem.Metadados with { Fontes = [] } }, "CORPO_E_SAUDE"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void ComplementoMantemZerosPublicadosSemInventarFracaoDePessoas(int percentual)
    {
        var leitura = Assert.Single(Ler(Snapshot(Estudo("a", percentual))));
        Assert.Equal([percentual, 100m - percentual], leitura.Grupos.Select(g => g.Percentual));
        Assert.DoesNotContain("1 em cada", leitura.Leitura);
    }
}
