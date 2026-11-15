using FluentValidation;
using Icarus.Application.UseCases.ConsultarExtratoPontos;

namespace Icarus.Api.Contracts.ExtratoPontos;

public sealed record ConsultarExtratoPontosRequest
{
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
    public bool TodoHistorico { get; init; }
    public NaturezaExtrato Natureza { get; init; }
    public int Limite { get; init; } = 20;
    public string? Cursor { get; init; }
}
public sealed class ConsultarExtratoPontosRequestValidator : AbstractValidator<ConsultarExtratoPontosRequest>
{
    public ConsultarExtratoPontosRequestValidator()
    {
        RuleFor(r => r.Limite).InclusiveBetween(1, 50);
        RuleFor(r => r.Natureza).IsInEnum();
        RuleFor(r => r.Cursor).MaximumLength(4096);
        RuleFor(r => r).Must(r => (r.Inicio is null) == (r.Fim is null)).WithMessage("Informe o início e o fim do período.");
        RuleFor(r => r).Must(r => !r.TodoHistorico || r.Inicio is null).WithMessage("Escolha um período ou todo o histórico.");
        RuleFor(r => r).Must(r => r.Fim != DateOnly.MaxValue && (r.Inicio is null
            || (r.Inicio <= r.Fim && r.Fim!.Value.DayNumber - r.Inicio.Value.DayNumber <= 3660)))
            .WithMessage("Confira o período informado.");
    }
}
