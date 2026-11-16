using System.Text.Json;
using Icarus.Api.Contracts.Diario;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Diario;

public sealed class EntradaDiarioRequestValidatorTests
{
    private readonly EntradaDiarioRequestValidator _validador = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RecusaTextoVazio(string? texto) =>
        Assert.False(_validador.Validate(new EntradaDiarioRequest(TipoEntradaDiario.Reflexao, new(2026, 10, 20), texto!)).IsValid);

    [Fact]
    public void ExigeTipoDataETextoDentroDoLimite()
    {
        Assert.False(_validador.Validate(new EntradaDiarioRequest(null, new(2026, 10, 20), "Registro")).IsValid);
        Assert.False(_validador.Validate(new EntradaDiarioRequest((TipoEntradaDiario)99, new(2026, 10, 20), "Registro")).IsValid);
        Assert.False(_validador.Validate(new EntradaDiarioRequest(TipoEntradaDiario.Reflexao, default, "Registro")).IsValid);
        Assert.False(_validador.Validate(new EntradaDiarioRequest(TipoEntradaDiario.Reflexao, new(2026, 10, 20), new('a', 10001))).IsValid);
        Assert.True(_validador.Validate(new EntradaDiarioRequest(TipoEntradaDiario.Deslize, new(2026, 1, 1), new('a', 10000))).IsValid);
    }

    [Theory]
    [InlineData("usuarioId", "\"identidade-forjada\"")]
    [InlineData("audio", "\"arquivo-local\"")]
    [InlineData("saldoPontos", "999")]
    public void RecusaCamposForaDoContrato(string campo, string valor)
    {
        var corpo = "{\"Tipo\":0,\"DataRegistro\":\"2026-10-20\",\"Texto\":\"Registro\",\"" + campo + "\":" + valor + "}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EntradaDiarioRequest>(corpo));
    }
}
