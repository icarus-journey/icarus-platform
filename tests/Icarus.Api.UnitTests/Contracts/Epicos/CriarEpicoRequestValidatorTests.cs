using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Epicos;
using Icarus.Domain.Enums;

namespace Icarus.Api.UnitTests.Contracts.Epicos;

public class CriarEpicoRequestValidatorTests
{
    private readonly CriarEpicoRequestValidator _validator = new();

    private static CriarEpicoRequest RequisicaoValida() => new(
        "Ser promovido", "descrição", AreaDaVida.TrabalhoECarreira, Dificuldade.Dificil, null, null);

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

    [Fact(DisplayName = "Prazos omitidos não geram erro")]
    public void Validate_DeveAceitarPrazosOmitidos()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { PrazoInicio = null, PrazoFim = null });

        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
