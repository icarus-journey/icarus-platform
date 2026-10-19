using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Missoes;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Missoes;

public class CriarMissaoRequestValidatorTests
{
    private readonly CriarMissaoRequestValidator _validator = new();

    private static CriarMissaoRequest RequisicaoValida() => new(
        null, "Caminhada", "descrição", "benefício", Dificuldade.Facil, null, null);

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Título vazio é rejeitado")]
    public void Validate_DeveRejeitarTituloVazio()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Titulo = "" });

        resultado.ShouldHaveValidationErrorFor(r => r.Titulo);
    }

    [Fact(DisplayName = "Horário em formato inválido é rejeitado")]
    public void Validate_DeveRejeitarHorarioInvalido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Horario = "25:99" });

        resultado.ShouldHaveValidationErrorFor(r => r.Horario);
    }

    [Fact(DisplayName = "Horário omitido não gera erro")]
    public void Validate_DeveAceitarHorarioOmitido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Horario = null });

        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
