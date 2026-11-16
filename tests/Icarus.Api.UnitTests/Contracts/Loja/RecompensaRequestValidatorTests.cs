using Icarus.Api.Contracts.Loja;

namespace Icarus.Api.UnitTests.Contracts.Loja;

public sealed class RecompensaRequestValidatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void RejeitaCustoForaDoLimite(int custo)
        => Assert.False(new RecompensaRequestValidator().Validate(new RecompensaRequest("Descanso", null, custo)).IsValid);

    [Fact]
    public void ValidaTextosELimitesDoContrato()
    {
        var validador = new RecompensaRequestValidator();
        Assert.True(validador.Validate(new RecompensaRequest(new string('a', 120), new string('b', 1000), 1_000_000)).IsValid);
        Assert.False(validador.Validate(new RecompensaRequest(" ", null, 1)).IsValid);
        Assert.False(validador.Validate(new RecompensaRequest(new string('a', 121), null, 1)).IsValid);
        Assert.False(validador.Validate(new RecompensaRequest("Descanso", new string('a', 1001), 1)).IsValid);
        Assert.False(new ResgateRequestValidator().Validate(new ResgateRequest(Guid.Empty)).IsValid);
    }
}
