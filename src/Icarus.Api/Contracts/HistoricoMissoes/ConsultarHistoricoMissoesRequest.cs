using Icarus.Application.UseCases.ConsultarHistoricoMissoes;
using FluentValidation;

namespace Icarus.Api.Contracts.HistoricoMissoes;

public sealed record ConsultarHistoricoMissoesRequest
{
    public DateOnly? Inicio { get; init; }
    public DateOnly? Fim { get; init; }
    public EstadoHistoricoMissao Estado { get; init; }
    public long? CampanhaId { get; init; }
    public bool Avulsas { get; init; }
    public int Limite { get; init; } = 20;
    public string? Cursor { get; init; }
}
public sealed class ConsultarHistoricoMissoesRequestValidator : AbstractValidator<ConsultarHistoricoMissoesRequest>
{
    public ConsultarHistoricoMissoesRequestValidator()
    {
        RuleFor(r => r.Limite).InclusiveBetween(1, 50);
        RuleFor(r => r.Estado).IsInEnum();
        RuleFor(r => r.CampanhaId).GreaterThan(0).When(r => r.CampanhaId is not null);
        RuleFor(r => r.Cursor).MaximumLength(4096);
        RuleFor(r => r).Must(r => !r.Avulsas || r.CampanhaId is null).WithMessage("Escolha uma campanha ou missões avulsas.");
        RuleFor(r => r).Must(r => (r.Inicio is null) == (r.Fim is null)).WithMessage("Informe o início e o fim do período.");
        RuleFor(r => r).Must(r => r.Inicio is null || (r.Inicio <= r.Fim && r.Fim!.Value.DayNumber - r.Inicio.Value.DayNumber <= 3660))
            .WithMessage("Confira o período informado.");
    }
}
