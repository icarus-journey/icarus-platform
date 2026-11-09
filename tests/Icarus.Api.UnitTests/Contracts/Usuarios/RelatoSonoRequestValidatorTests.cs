using System.Text.Json;
using Icarus.Api.Contracts.Usuarios;

namespace Icarus.Api.UnitTests.Contracts.Usuarios;

public sealed class RelatoSonoRequestValidatorTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(0, null, true)]
    [InlineData(360, 420, true)]
    [InlineData(1440, 1440, true)]
    [InlineData(-1, null, false)]
    [InlineData(1441, null, false)]
    [InlineData(null, 420, false)]
    [InlineData(360, 0, false)]
    [InlineData(360, 1441, false)]
    public void ExigeRelatoExplicitoELimitesSemPrescreverMeta(int? habitual, int? meta, bool valido)
        => Assert.Equal(valido, new AtualizarRelatoSonoRequestValidator().Validate(new AtualizarRelatoSonoRequest(habitual, meta, Guid.Empty)).IsValid);

    [Fact]
    public void CamposDePerfilNaoEntramNesteContrato()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AtualizarRelatoSonoRequest>("{\"saldoPontos\":100}", JsonSerializerOptions.Web));
}
