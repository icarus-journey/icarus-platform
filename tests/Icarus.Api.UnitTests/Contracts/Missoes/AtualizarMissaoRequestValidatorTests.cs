using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Missoes;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Missoes;

public class AtualizarMissaoRequestValidatorTests
{
    private readonly AtualizarMissaoRequestValidator _validator = new();

    private static AtualizarMissaoRequest RequisicaoValida() => new(
        "Caminhada", "descrição", "benefício", Dificuldade.Facil, null, "07:00");

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Horário em formato inválido é rejeitado")]
    public void Validate_DeveRejeitarHorarioInvalido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Horario = "não é hora" });

        resultado.ShouldHaveValidationErrorFor(r => r.Horario);
    }
}
