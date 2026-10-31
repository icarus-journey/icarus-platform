using FluentValidation;

namespace Icarus.Api.Contracts.Missoes;

public sealed record ConcluirMissaoRequest(long? OcorrenciaId);

public sealed class ConcluirMissaoRequestValidator : AbstractValidator<ConcluirMissaoRequest>
{
    public ConcluirMissaoRequestValidator() => RuleFor(r => r.OcorrenciaId).GreaterThan(0).When(r => r.OcorrenciaId is not null);
}
