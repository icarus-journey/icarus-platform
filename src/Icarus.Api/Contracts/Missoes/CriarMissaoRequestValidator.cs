using FluentValidation;
using Icarus.Domain.Enums;

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

        RuleFor(r => r.Recorrencia).IsInEnum().When(r => r.Recorrencia is not null);

        // RF-04.4: "tipo ou parâmetro inválido é rejeitado" — semanal sem dia nenhum marcado não tem como gerar ocorrência.
        RuleFor(r => r.DiasSemana)
            .NotEmpty()
            .When(r => r.Recorrencia == TipoRecorrencia.Semanal)
            .WithMessage("Informe ao menos um dia da semana para recorrência semanal.");

        RuleFor(r => r.DataFimRecorrencia)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(r => r.Recorrencia is not null && r.DataFimRecorrencia is not null)
            .WithMessage("A data limite da recorrência não pode estar no passado.");
    }
}
