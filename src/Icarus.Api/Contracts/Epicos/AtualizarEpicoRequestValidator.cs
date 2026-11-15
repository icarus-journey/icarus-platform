using FluentValidation;

namespace Icarus.Api.Contracts.Epicos;

public sealed class AtualizarEpicoRequestValidator : AbstractValidator<AtualizarEpicoRequest>
{
    public AtualizarEpicoRequestValidator()
    {
        RuleFor(r => r.Titulo)
            .NotEmpty().WithName("Titulo")
            .MaximumLength(200);

        RuleFor(r => r.Dificuldade).IsInEnum();

        RuleFor(r => r.PrazoFim)
            .GreaterThanOrEqualTo(r => r.PrazoInicio)
            .WithMessage("O prazo fim não pode ser anterior ao prazo início.");
    }
}
