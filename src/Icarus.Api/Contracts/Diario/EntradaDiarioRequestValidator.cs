using FluentValidation;

namespace Icarus.Api.Contracts.Diario;

public sealed class EntradaDiarioRequestValidator : AbstractValidator<EntradaDiarioRequest>
{
    public EntradaDiarioRequestValidator()
    {
        RuleFor(entrada => entrada.Tipo).NotNull().IsInEnum();
        RuleFor(entrada => entrada.DataRegistro).NotEmpty();
        RuleFor(entrada => entrada.Texto).NotEmpty().MaximumLength(10000);
    }
}
