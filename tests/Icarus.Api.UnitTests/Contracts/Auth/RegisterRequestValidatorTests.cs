using FluentValidation.TestHelper;
using Icarus.Api.Contracts.Auth;

namespace Icarus.Api.UnitTests.Contracts.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest RequisicaoValida()
        => new("Ana Silva", "ana@exemplo.com", "minhaSenha123", new DateOnly(2000, 1, 1));

    [Fact(DisplayName = "Requisição válida não gera nenhum erro")]
    public void Validate_DeveAceitarRequisicaoValida()
    {
        var resultado = _validator.TestValidate(RequisicaoValida());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Senha com menos de 8 caracteres é rejeitada (RF-01.2)")]
    public void Validate_DeveRejeitarSenhaCurta()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Senha = "curta12" });

        resultado.ShouldHaveValidationErrorFor(r => r.Senha);
    }

    [Fact(DisplayName = "Senha com mais de 128 caracteres é rejeitada (RF-01.2)")]
    public void Validate_DeveRejeitarSenhaLonga()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Senha = new string('a', 129) });

        resultado.ShouldHaveValidationErrorFor(r => r.Senha);
    }

    [Fact(DisplayName = "Senha só com letras minúsculas é aceita: não há regra de composição (RF-01.2)")]
    public void Validate_DeveAceitarSenhaSemRegraDeComposicao()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Senha = "somenteminusculas" });

        resultado.ShouldNotHaveValidationErrorFor(r => r.Senha);
    }

    [Fact(DisplayName = "E-mail em formato inválido é rejeitado")]
    public void Validate_DeveRejeitarEmailInvalido()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Email = "isso-nao-e-email" });

        resultado.ShouldHaveValidationErrorFor(r => r.Email);
    }

    [Fact(DisplayName = "Nome vazio é rejeitado")]
    public void Validate_DeveRejeitarNomeVazio()
    {
        var resultado = _validator.TestValidate(RequisicaoValida() with { Nome = "" });

        resultado.ShouldHaveValidationErrorFor(r => r.Nome);
    }
}
