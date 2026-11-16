using System.Text.Json;
using Icarus.Api.Contracts.Campanhas;

namespace Icarus.Api.UnitTests.Contracts.Campanhas;

public sealed class RegistrarProgressoCampanhaRequestValidatorTests
{
    private readonly RegistrarProgressoCampanhaRequestValidator _validador = new();

    [Theory]
    [InlineData(null)]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000")]
    public void RecusaValoresAusentesNegativosOuForaDaPrecisao(string? texto)
    {
        var valor = texto is null ? (decimal?)null : decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(_validador.Validate(new RegistrarProgressoCampanhaRequest(valor, "versao", Guid.NewGuid())).IsValid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("3.5")]
    [InlineData("9999999999.99")]
    public void AceitaLimitesEFracoes(string texto) => Assert.True(_validador.Validate(new RegistrarProgressoCampanhaRequest(decimal.Parse(texto, System.Globalization.CultureInfo.InvariantCulture), "versao", Guid.NewGuid())).IsValid);

    [Fact]
    public void ExigeVersaoEIdentificadorDeTentativa()
    {
        Assert.False(_validador.Validate(new RegistrarProgressoCampanhaRequest(3, "", Guid.NewGuid())).IsValid);
        Assert.False(_validador.Validate(new RegistrarProgressoCampanhaRequest(3, "versao", Guid.Empty)).IsValid);
        Assert.False(_validador.Validate(new RegistrarProgressoCampanhaRequest(3, new('a', 61), Guid.NewGuid())).IsValid);
    }

    [Theory]
    [InlineData("UsuarioId")]
    [InlineData("SaldoPontos")]
    [InlineData("RegistradaEm")]
    public void RejeitaCamposQueNaoSaoDoContrato(string campo)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RegistrarProgressoCampanhaRequest>("{\"ValorAtual\":3,\"VersaoEsperada\":\"versao\",\"OperacaoId\":\"f0cd2e93-0901-4210-b045-4c22ac960bdc\",\"" + campo + "\":1}"));
}
