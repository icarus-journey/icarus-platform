using FluentValidation;

namespace Icarus.Api.Contracts.Campanhas;

public sealed class AtualizarCampanhaRequestValidator : AbstractValidator<AtualizarCampanhaRequest>
{
    public AtualizarCampanhaRequestValidator()
    {
        RuleFor(r => r.Titulo)
            .NotEmpty().WithName("Titulo")
            .MaximumLength(200);

        RuleFor(r => r.Dificuldade).IsInEnum();

        RuleFor(r => r.PrazoFim)
            .GreaterThanOrEqualTo(r => r.PrazoInicio)
            .WithMessage("O prazo fim não pode ser anterior ao prazo início.");

        RuleFor(r => r.ValorMensurado).GreaterThanOrEqualTo(0).When(r => r.ValorMensurado is not null);
        RuleFor(r => r.Unidade).MaximumLength(30);
    }
}
