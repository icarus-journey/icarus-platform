using FluentValidation;

namespace Icarus.Api.Contracts.Usuarios;

public sealed class AtualizarRelatoSonoRequestValidator : AbstractValidator<AtualizarRelatoSonoRequest>
{
    public AtualizarRelatoSonoRequestValidator()
    {
        RuleFor(p => p.HabitualMinutos).InclusiveBetween(0, 1440).When(p => p.HabitualMinutos is not null);
        RuleFor(p => p.MetaMinutos).InclusiveBetween(1, 1440).When(p => p.MetaMinutos is not null);
        RuleFor(p => p.MetaMinutos).Null().When(p => p.HabitualMinutos is null);
    }
}
