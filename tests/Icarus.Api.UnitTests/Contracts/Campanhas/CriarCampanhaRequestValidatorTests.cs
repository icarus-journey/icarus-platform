using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Campanhas;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Campanhas;

public class CriarCampanhaRequestValidatorTests
{
    private readonly CriarCampanhaRequestValidator _validator = new();

    private static CriarCampanhaRequest RequisicaoValida() => new(
        1, "Campanha", "descrição", Dificuldade.Facil, null, null, null, null);

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "EpicoId inválido é rejeitado")]
    public void Validate_DeveRejeitarEpicoIdInvalido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { EpicoId = 0 });

        resultado.ShouldHaveValidationErrorFor(r => r.EpicoId);
    }

    [Fact(DisplayName = "Título vazio é rejeitado")]
    public void Validate_DeveRejeitarTituloVazio()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Titulo = "" });

        resultado.ShouldHaveValidationErrorFor(r => r.Titulo);
    }

    [Fact(DisplayName = "Valor mensurado negativo é rejeitado")]
    public void Validate_DeveRejeitarValorMensuradoNegativo()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { ValorMensurado = -1 });

        resultado.ShouldHaveValidationErrorFor(r => r.ValorMensurado);
    }
}
