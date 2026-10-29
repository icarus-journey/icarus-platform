using Icarus.Application.Common.Analise;

namespace Icarus.Application.UnitTests.Analise;

public sealed class SelecionadorReferenciasPublicasTests
{
    private static ReferenciaPublica Referencia(string faixa, bool? estuda = null, bool? trabalha = null,
        string tipo = "todos", string indicador = "atus_tarefas_estudo") => new()
    {
        Id = $"{faixa}-{estuda}-{trabalha}-{tipo}", Indicador = indicador, Medida = "duracao_participantes",
        FaixaEtaria = faixa, TipoDia = tipo, Filtros = new(estuda, trabalha)
    };

    [Fact]
    public void PriorizaFaixaAntesDePerfilEPerfilNaOrdemDoCatalogo()
    {
        var amplo = Referencia("18–28", true, true);
        var trabalho = Referencia("25–28", null, true);
        var estudo = Referencia("25–28", true);
        var semFiltro = Referencia("25–28");
        Assert.Same(trabalho, SelecionadorReferenciasPublicas.Selecionar([amplo, estudo, trabalho, semFiltro],
            "atus_tarefas_estudo", "duracao_participantes", 25, true, true));
        Assert.Same(semFiltro, SelecionadorReferenciasPublicas.Selecionar([amplo, semFiltro],
            "atus_tarefas_estudo", "duracao_participantes", 25, true, true));
    }

    [Fact]
    public void AusenciaNaoViraRespostaNegativaEQuedaEtariaPreservaMenores()
    {
        var negativa = Referencia("16–17", false, false);
        var adolescente = Referencia("15–17");
        var adulto = Referencia("18+");
        Assert.Same(adolescente, SelecionadorReferenciasPublicas.Selecionar([negativa, adulto, adolescente],
            "atus_tarefas_estudo", "duracao_participantes", 16));
        Assert.Null(SelecionadorReferenciasPublicas.Selecionar([adulto], "atus_tarefas_estudo", "duracao_participantes", 16));
        Assert.Null(SelecionadorReferenciasPublicas.Selecionar([adolescente], "atus_tarefas_estudo", "duracao_participantes", 14));
    }

    [Fact]
    public void DiaSolicitadoTemPrioridadeEInformaAmpliacaoSomenteDepoisDeEsgotarRecortes()
    {
        var folhaTodos = Referencia("25–28");
        var amploDia = Referencia("18+", tipo: "segunda_sexta");
        Assert.Same(amploDia, SelecionadorReferenciasPublicas.Selecionar([folhaTodos, amploDia],
            "atus_tarefas_estudo", "duracao_participantes", 25, tipoDia: "segunda_sexta"));
        Assert.Same(folhaTodos, SelecionadorReferenciasPublicas.Selecionar([folhaTodos],
            "atus_tarefas_estudo", "duracao_participantes", 25, tipoDia: "sabado_domingo"));
    }

    [Fact]
    public void NaoUsaMatriculaNoPaiAdultoNemInventaFiltroVigitel()
    {
        var invalida = Referencia("18+", true);
        var ampla = Referencia("18+");
        Assert.Same(ampla, SelecionadorReferenciasPublicas.Selecionar([invalida, ampla],
            "atus_tarefas_estudo", "duracao_participantes", 40, true));
        var vigitel = Referencia("18–28", indicador: "vigitel_duracao_sono_habitual");
        Assert.Same(vigitel, SelecionadorReferenciasPublicas.Selecionar([vigitel],
            "vigitel_duracao_sono_habitual", "duracao_participantes", 25, true, false));
        Assert.Null(SelecionadorReferenciasPublicas.Selecionar([vigitel],
            "vigitel_duracao_sono_habitual", "duracao_participantes", 17));
    }

    [Fact]
    public void NaoTrocaIndicadorMedidaOuFonteERejeitaTipoDeDiaDesconhecido()
    {
        Assert.Null(SelecionadorReferenciasPublicas.Selecionar([Referencia("18+")], "atus_sono_diario", "duracao_participantes", 30));
        Assert.Throws<ArgumentException>(() => SelecionadorReferenciasPublicas.Selecionar([], "atus_sono_diario", "percentual", 30, tipoDia: "feriado"));
    }
}
