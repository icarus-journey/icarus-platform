using FluentValidation;

namespace Icarus.Api.Contracts.Campanhas;

public sealed class CriarCampanhaRequestValidator : AbstractValidator<CriarCampanhaRequest>
{
    public CriarCampanhaRequestValidator()
    {
        RuleFor(r => r.EpicoId).GreaterThan(0);

        RuleFor(r => r.Titulo)
            .NotEmpty().WithName("Titulo")
            .MaximumLength(200);

        RuleFor(r => r.Dificuldade).IsInEnum();

        When(r => r.PrazoInicio is not null && r.PrazoFim is not null, () =>
        {
            RuleFor(r => r.PrazoFim)
                .Must((request, prazoFim) => prazoFim!.Value >= request.PrazoInicio!.Value)
                .WithMessage("O prazo fim não pode ser anterior ao prazo início.");
        });

        RuleFor(r => r.ValorMensurado).GreaterThanOrEqualTo(0).When(r => r.ValorMensurado is not null);
        RuleFor(r => r.Unidade).MaximumLength(30);
    }
}
