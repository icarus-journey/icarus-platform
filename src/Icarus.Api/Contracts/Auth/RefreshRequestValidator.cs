using FluentValidation;

namespace Icarus.Api.Contracts.Auth;

/// <summary>Validação de formato, aplicada na borda HTTP pelo <see cref="Filters.ValidationFilter"/>.</summary>
public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(r => r.RefreshToken)
            .NotEmpty().WithName("Refresh token");
    }
}
