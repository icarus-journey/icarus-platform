using FluentValidation;
namespace Icarus.Api.Contracts.HistoricoRelatorios;

public sealed record ConsultaHistoricoRelatoriosRequest(int Limite = 20, string? Cursor = null);
public sealed class ConsultaHistoricoRelatoriosRequestValidator : AbstractValidator<ConsultaHistoricoRelatoriosRequest>
{
    public ConsultaHistoricoRelatoriosRequestValidator()
    {
        RuleFor(r => r.Limite).InclusiveBetween(1, 50);
        RuleFor(r => r.Cursor).MaximumLength(4096);
    }
}
