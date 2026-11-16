using FluentValidation;

namespace Icarus.Api.Contracts.Analise;

public sealed record ConsultarProgressoRequest(string Periodo = "semanaAnterior");
public sealed class ConsultarProgressoRequestValidator : AbstractValidator<ConsultarProgressoRequest>
{
    public ConsultarProgressoRequestValidator() => RuleFor(r => r.Periodo)
        .Must(periodo => periodo is "semanaAnterior" or "mesAnterior" or "seisMeses")
        .WithMessage("Escolha semana anterior, mês anterior ou últimos seis meses completos.");
}
