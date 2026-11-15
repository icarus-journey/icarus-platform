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

    [Theory]
    [InlineData(null, 1, true)]
    [InlineData(null, 0, false)]
    [InlineData(null, -1, false)]
    [InlineData("SONO_E_RECUPERACAO", 1, false)]
    public void CampanhaPositivaSubstituiAreaSemPermitirConsultasAmbiguas(string? area, long campanha, bool valido)
        => Assert.Equal(valido, new ConsultarReferenciasPublicasRequestValidator().Validate(new ConsultarReferenciasPublicasRequest(area, CampanhaId: campanha)).IsValid);
}
