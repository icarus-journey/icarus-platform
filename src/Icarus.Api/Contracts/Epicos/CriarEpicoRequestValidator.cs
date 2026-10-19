using FluentValidation;

namespace Icarus.Api.Contracts.Epicos;

public sealed class CriarEpicoRequestValidator : AbstractValidator<CriarEpicoRequest>
{
    public CriarEpicoRequestValidator()
    {
        RuleFor(r => r.Titulo)
            .NotEmpty().WithName("Titulo")
            .MaximumLength(200);

        RuleFor(r => r.AreaDaVida).IsInEnum();
        RuleFor(r => r.Dificuldade).IsInEnum();

        When(r => r.PrazoInicio is not null && r.PrazoFim is not null, () =>
        {
            RuleFor(r => r.PrazoFim)
                .Must((request, prazoFim) => prazoFim!.Value >= request.PrazoInicio!.Value)
                .WithMessage("O prazo fim não pode ser anterior ao prazo início.");
        });
    }
}
