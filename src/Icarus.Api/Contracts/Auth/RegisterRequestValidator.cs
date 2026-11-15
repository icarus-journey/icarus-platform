using FluentValidation;

namespace Icarus.Api.Contracts.Auth;

/// <summary>
/// Regras do RF-01/RF-01.1/RF-01.2, aplicadas na borda HTTP pelo <see cref="Filters.ValidationFilter"/>.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Nome)
            .NotEmpty().WithName("Nome")
            .MaximumLength(200);

        RuleFor(r => r.Email)
            .NotEmpty().WithName("E-mail")
            .EmailAddress()
            .MaximumLength(320);

        // RF-01.2: 8 a 128 caracteres, sem regra obrigatória de composição.
        RuleFor(r => r.Senha)
            .NotEmpty().WithName("Senha")
            .Length(8, 128);

        RuleFor(r => r.DataNascimento)
            .NotEmpty().WithName("Data de nascimento");
    }
}
