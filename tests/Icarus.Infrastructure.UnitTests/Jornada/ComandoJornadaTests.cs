using System.Text;
using System.Text.Json;
using Icarus.Infrastructure.Messaging;

namespace Icarus.Infrastructure.UnitTests.Jornada;

public sealed class ComandoJornadaTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"versaoContrato\":\"2.0.0\",\"trabalhoId\":null}")]
    [InlineData("{\"versaoContrato\":\"1.0.0\"}")]
    [InlineData("{\"versaoContrato\":\"1.0.0\",\"trabalhoId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"versaoContrato\":\"1.0.0\",\"trabalhoId\":null,\"usuarioId\":\"nao-permitido\"}")]
    public void RejeitaContratoInvalidoOuCamposNaoPermitidos(string carga)
        => Assert.Throws<JsonException>(() => ComandoJornada.Ler(Encoding.UTF8.GetBytes(carga)));

    [Fact]
    public void AceitaSomenteCicloOuIdentificadorOpaco()
    {
        var ciclo = new ComandoJornada("1.0.0", null);
        Assert.Equal(ciclo, ComandoJornada.Ler(JsonSerializer.SerializeToUtf8Bytes(ciclo, ComandoJornada.Json)));
        var trabalho = new ComandoJornada("1.0.0", Guid.NewGuid());
        Assert.Equal(trabalho, ComandoJornada.Ler(JsonSerializer.SerializeToUtf8Bytes(trabalho, ComandoJornada.Json)));
        Assert.Throws<JsonException>(() => ComandoJornada.Ler(new byte[1025]));
    }
}
