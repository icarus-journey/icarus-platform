using FluentValidation;

namespace Icarus.Api.Contracts.Missoes;

public sealed class CriarMissaoRequestValidator : AbstractValidator<CriarMissaoRequest>
{
    public CriarMissaoRequestValidator()
    {
        RuleFor(r => r.CampanhaId).GreaterThan(0).When(r => r.CampanhaId is not null);

        RuleFor(r => r.Titulo)
            .NotEmpty().WithName("Titulo")
            .MaximumLength(200);

        RuleFor(r => r.Dificuldade).IsInEnum();

        RuleFor(r => r.Horario)
            .Must(h => TimeOnly.TryParse(h, out _))
            .When(r => r.Horario is not null)
            .WithMessage("Informe um horário válido no formato HH:mm.");
    }
}
