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
        Assert.Equal(ContratoRelatorioSemanal.Versao, pedido.VersaoContrato);
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
}
