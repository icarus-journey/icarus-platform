using FluentValidation;
using Icarus.Application.Common.Analise;

namespace Icarus.Api.Contracts.Analise;

public sealed record ConsultarReferenciasPublicasRequest(string? AreaDaVida = null, string TipoDia = "todos", long? CampanhaId = null);
public sealed class ConsultarReferenciasPublicasRequestValidator : AbstractValidator<ConsultarReferenciasPublicasRequest>
{
    public ConsultarReferenciasPublicasRequestValidator()
    {
        RuleFor(r => r.AreaDaVida).Must(codigo => codigo is null || CatalogoAreasAnaliticas.Areas.Any(a => a.Codigo == codigo))
            .WithMessage("Escolha uma área da vida válida.");
        RuleFor(r => r.TipoDia).Must(tipo => tipo is "todos" or "segunda_sexta" or "sabado_domingo")
            .WithMessage("Escolha todos os dias, segunda a sexta ou sábado e domingo.");
        RuleFor(r => r.CampanhaId).GreaterThan(0).When(r => r.CampanhaId.HasValue);
        RuleFor(r => r.AreaDaVida).Null().When(r => r.CampanhaId.HasValue)
            .WithMessage("Escolha uma campanha ou uma área para consultar as pesquisas.");
    }
}
