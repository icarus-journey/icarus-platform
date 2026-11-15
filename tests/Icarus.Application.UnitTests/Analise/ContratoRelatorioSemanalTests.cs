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
        Assert.Equal("1.1.0", pedido.VersaoContrato);
        Assert.Equal(8, pedido.ComparacaoSemanal!.Semana.Concluidas);
        Assert.Equal(9, pedido.Indicadores.Concluidas);
        Assert.Equal(56.2m, pedido.ComparacaoSemanal.MediaQuatroSemanas.TaxaConclusao);
        Assert.Null(JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(Exemplo("solicitacao"), ContratoRelatorioSemanal.Json)!.ComparacaoSemanal);
    }

    [Fact]
    public void OrientacaoPodeEstarAusenteSoNaVersaoNovaESaudeNaoEInferida()
    {
        var texto = new TextoRelatorioSemanal("Mais conclusões registradas", "O volume cresceu e a proporção ficou próxima da anterior.", null, "Qual missão continua sendo prioridade?");
        Assert.True(ContratoRelatorioSemanal.ConteudoValido(texto, "1.2.0"));
        Assert.False(ContratoRelatorioSemanal.ConteudoValido(texto, "1.1.0"));
        Assert.False(ContratoRelatorioSemanal.ConteudoValido(texto with { Resumo = "Você terá sobrecarga psicológica." }, "1.2.0"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(Exemplo("solicitacao").Replace("\"indicadores\": {", "\"sonoHabitualMinutos\":360,\"indicadores\": {"), ContratoRelatorioSemanal.Json));
    }

    [Fact]
    public void RejeitaNumeroInventadoSemAplicarRegraNovaAoLegado()
    {
        var indicadores = new Icarus.Application.Common.Analise.IndicadoresPessoais(null, 2, null, null, null, null, null, null, null, null, null, [], null, null, null, 1, 2, true, []);
        var texto = new TextoRelatorioSemanal("Conclusões", "Você registrou 2 conclusões.", null, "Qual prioridade?");
        Assert.True(ContratoRelatorioSemanal.NumerosSustentados(texto, indicadores, null));
        var relatorio = new Icarus.Domain.Entities.Relatorio { PeriodoInicio = new(2026, 10, 12), PeriodoFim = new(2026, 10, 18),
            Conteudo = JsonSerializer.Serialize(new SnapshotRelatorioSemanal(texto, indicadores, new("ollama", "teste"), VersaoIndicadores: "1.2.0"), ContratoRelatorioSemanal.Json) };
        Assert.NotNull(Icarus.Application.UseCases.ConsultarHistoricoRelatorios.ApresentacaoHistoricoRelatorios.Ler(relatorio));
        Assert.False(ContratoRelatorioSemanal.NumerosSustentados(texto with { Resumo = "Você registrou 9999 conclusões." }, indicadores, null));
        Assert.True(ContratoRelatorioSemanal.ConteudoValido(texto with { ProximoAjuste = "Reflita sobre sua dedicação." }, "1.1.0"));
    }
}
