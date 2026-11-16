using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Campanhas;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Campanhas;

public class AtualizarCampanhaRequestValidatorTests
{
    private readonly AtualizarCampanhaRequestValidator _validator = new();

    private static AtualizarCampanhaRequest RequisicaoValida() => new(
        "Campanha", "descrição", Dificuldade.Facil, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), null, null);

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Prazo fim anterior ao prazo início é rejeitado")]
    public void Validate_DeveRejeitarPrazoFimAnteriorAoPrazoInicio()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with
        {
            PrazoInicio = new DateOnly(2026, 6, 1),
            PrazoFim = new DateOnly(2026, 1, 1)
        });

        resultado.ShouldHaveValidationErrorFor(r => r.PrazoFim);
    }
}
