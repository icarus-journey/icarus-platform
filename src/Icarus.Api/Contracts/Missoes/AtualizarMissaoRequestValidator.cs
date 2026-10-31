using FluentValidation;
using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

public sealed class AtualizarMissaoRequestValidator : AbstractValidator<AtualizarMissaoRequest>
{
    public AtualizarMissaoRequestValidator()
    {
        RuleFor(r => r.Recorrencia).IsInEnum().When(r => r.Recorrencia is not null);
        RuleFor(r => r.DiasSemana).NotEmpty().When(r => r.Recorrencia == TipoRecorrencia.Semanal);
        RuleForEach(r => r.DiasSemana).IsInEnum();
        RuleFor(r => r.DiaDoMes).InclusiveBetween(1, 31).When(r => r.DiaDoMes is not null);
        RuleFor(r => r.VersaoEsperada).NotEmpty().MaximumLength(40).When(r => r.Recorrencia is not null);
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
