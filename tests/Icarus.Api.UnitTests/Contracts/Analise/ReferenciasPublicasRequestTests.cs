using Icarus.Api.Contracts.Analise;

namespace Icarus.Api.UnitTests.Contracts.Analise;

public sealed class ReferenciasPublicasRequestTests
{
    [Theory]
    [InlineData(null, "todos", true)]
    [InlineData("SONO_E_RECUPERACAO", "segunda_sexta", true)]
    [InlineData("FINANCAS", "sabado_domingo", true)]
    [InlineData("inventada", "todos", false)]
    [InlineData("SONO_E_RECUPERACAO", "feriado", false)]
    public void ValidaSomenteAreasETiposDeDiaPermitidos(string? area, string dia, bool valido)
        => Assert.Equal(valido, new ConsultarReferenciasPublicasRequestValidator().Validate(new ConsultarReferenciasPublicasRequest(area, dia)).IsValid);
}
