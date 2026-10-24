using System.Text.Json;
using Icarus.Api.Contracts.Usuarios;

namespace Icarus.Api.UnitTests.Contracts.Usuarios;

public sealed class PerfilRequestValidatorTests
{
    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData(false, null, false, null)]
    [InlineData(true, 0d, true, 168d)]
    [InlineData(true, 20.25, true, 40.5)]
    public void AceitaAusenciaEValoresValidos(bool? estuda, double? estudo, bool? trabalha, double? trabalho)
    {
        var pedido = new AtualizarPerfilRequest(estuda, (decimal?)estudo, trabalha, (decimal?)trabalho);
        Assert.True(new AtualizarPerfilRequestValidator().Validate(pedido).IsValid);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(false, 10)]
    [InlineData(true, -1)]
    [InlineData(true, 168.01)]
    [InlineData(true, 2.345)]
    public void RejeitaCargaInconsistenteOuForaDosLimites(bool? situacao, double carga)
    {
        var estudo = new AtualizarPerfilRequest(situacao, (decimal)carga, null, null);
        var trabalho = new AtualizarPerfilRequest(null, null, situacao, (decimal)carga);
        Assert.False(new AtualizarPerfilRequestValidator().Validate(estudo).IsValid);
        Assert.False(new AtualizarPerfilRequestValidator().Validate(trabalho).IsValid);
    }

    [Theory]
    [InlineData("America/Sao_Paulo", true)]
    [InlineData("Europe/Lisbon", true)]
    [InlineData("UTC", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("Fuso/Inexistente", false)]
    [InlineData("E. South America Standard Time", false)]
    public void ExigeFusoIana(string? identificador, bool valido)
    {
        var pedido = new AtualizarFusoRequest(identificador!);
        Assert.Equal(valido, new AtualizarFusoRequestValidator().Validate(pedido).IsValid);
    }

    [Theory]
    [InlineData("nome")]
    [InlineData("email")]
    [InlineData("dataNascimento")]
    [InlineData("saldoPontos")]
    [InlineData("fusoHorario")]
    public void NaoPermiteAlterarOutrosCamposPeloPerfil(string campo)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object?> { [campo] = "valor" });
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AtualizarPerfilRequest>(json, JsonSerializerOptions.Web));
    }
}
