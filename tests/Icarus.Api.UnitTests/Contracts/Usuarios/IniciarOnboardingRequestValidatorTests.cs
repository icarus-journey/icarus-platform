using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Usuarios;

namespace Icarus.Api.UnitTests.Contracts.Usuarios;

public class IniciarOnboardingRequestValidatorTests
{
    private readonly IniciarOnboardingRequestValidator _validator = new();

    private static IniciarOnboardingRequest RequisicaoValida() => new(
        [new RespostaOnboardingRequest("Qual a sua faixa etária?", ["18 a 24"])]);

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Lista de respostas vazia é rejeitada")]
    public void Validate_DeveRejeitarListaVazia()
    {
        var resultado = _validator.TestValidate(new IniciarOnboardingRequest([]));

        resultado.ShouldHaveValidationErrorFor(r => r.Respostas);
    }

    [Fact(DisplayName = "Pergunta vazia é rejeitada")]
    public void Validate_DeveRejeitarPerguntaVazia()
    {
        var resultado = _validator.TestValidate(new IniciarOnboardingRequest([new RespostaOnboardingRequest("", ["18 a 24"])]));

        resultado.ShouldHaveValidationErrorFor("Respostas[0].Pergunta");
    }

    [Fact(DisplayName = "Lista de respostas de uma pergunta vazia é rejeitada")]
    public void Validate_DeveRejeitarRespostasVazia()
    {
        var resultado = _validator.TestValidate(new IniciarOnboardingRequest([new RespostaOnboardingRequest("Pergunta", [])]));

        resultado.ShouldHaveValidationErrorFor("Respostas[0].Respostas");
    }

    [Fact(DisplayName = "Resposta vazia dentro da lista de uma pergunta de múltipla escolha é rejeitada")]
    public void Validate_DeveRejeitarItemDeRespostaVazio()
    {
        var resultado = _validator.TestValidate(new IniciarOnboardingRequest([new RespostaOnboardingRequest("Pergunta", ["válida", ""])]));

        resultado.ShouldHaveValidationErrorFor("Respostas[0].Respostas[1]");
    }
}
