using FluentValidation;

namespace Icarus.Api.Contracts.Auth;

/// <summary>
/// Validação de formato, aplicada na borda HTTP pelo <see cref="Filters.ValidationFilter"/>.
/// Deliberadamente não valida o tamanho da senha (regra do RF-01.2, só se aplica ao
/// cadastro) — aqui só confirma que o corpo chegou bem formado; a senha errada (de
/// qualquer tamanho) é tratada como credencial inválida pelo caso de uso.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithName("E-mail")
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(r => r.Senha)
            .NotEmpty().WithName("Senha");
    }
}
