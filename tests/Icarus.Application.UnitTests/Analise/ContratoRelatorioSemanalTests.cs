using System.Text.Json;
using Icarus.Application.Common.Relatorios;

namespace Icarus.Application.UnitTests.Analise;

public class ContratoRelatorioSemanalTests
{
    private static string Exemplo(string nome) => File.ReadAllText(Path.Combine(
        AppContext.BaseDirectory, "Contratos", nome + ".exemplo.json"));

    [Fact]
    public void LeExemplosVersionadosCompartilhadosComTrabalhador()
    {
        var pedido = JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(Exemplo("solicitacao"), ContratoRelatorioSemanal.Json)!;
        var resultado = JsonSerializer.Deserialize<ResultadoRelatorioSemanal>(Exemplo("resultado"), ContratoRelatorioSemanal.Json)!;
        Assert.True(ContratoRelatorioSemanal.VersaoSuportada(pedido.VersaoContrato));
        Assert.Equal(pedido.IdMensagem, resultado.CausalidadeId);
        Assert.Equal(pedido.Periodo, resultado.Periodo);
        Assert.Null(pedido.Indicadores.TaxaConclusao);
        Assert.True(ContratoRelatorioSemanal.ConteudoValido(resultado.Conteudo));
        Assert.DoesNotContain("inicioUtc", JsonSerializer.Serialize(pedido, ContratoRelatorioSemanal.Json));
    }

    [Fact]
    public void RejeitaNovaEntradaPessoalEmContratoFechado()
    {
        var carga = Exemplo("solicitacao").Replace("\"indicadores\": {", "\"indicadores\": {\"email\":\"proibido@example.com\",");
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(carga, ContratoRelatorioSemanal.Json));
    }

    [Fact]
    public void LeExemploNovoSemReinterpretarAEntradaAntiga()
    {
        var carga = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contratos", "v1.1", "solicitacao.exemplo.json"));
        var pedido = JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(carga, ContratoRelatorioSemanal.Json)!;
        Assert.Equal(ContratoRelatorioSemanal.Versao, pedido.VersaoContrato);
        Assert.Equal(8, pedido.ComparacaoSemanal!.Semana.Concluidas);
        Assert.Equal(9, pedido.Indicadores.Concluidas);
        Assert.Equal(56.2m, pedido.ComparacaoSemanal.MediaQuatroSemanas.TaxaConclusao);
        Assert.Null(JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(Exemplo("solicitacao"), ContratoRelatorioSemanal.Json)!.ComparacaoSemanal);
    }
}
