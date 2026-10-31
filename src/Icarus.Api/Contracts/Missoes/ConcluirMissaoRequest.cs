using FluentValidation;

namespace Icarus.Api.Contracts.Missoes;

public sealed record ConcluirMissaoRequest(long? OcorrenciaId = null, DateOnly? DataRealizacao = null,
    TimeOnly? HorarioRealizacao = null, DateOnly? DataPrevistaEsperada = null, string? FusoHorarioEsperado = null);

public sealed class ConcluirMissaoRequestValidator : AbstractValidator<ConcluirMissaoRequest>
{
    public ConcluirMissaoRequestValidator()
    {
        RuleFor(r => r.FusoHorarioEsperado).MaximumLength(100);
        RuleFor(r => r.OcorrenciaId).GreaterThan(0).When(r => r.OcorrenciaId is not null);
        RuleFor(r => r.DataRealizacao).NotNull().When(r => r.HorarioRealizacao is not null)
            .WithMessage("Informe a data da realização.");
        RuleFor(r => r.HorarioRealizacao).NotNull().When(r => r.DataRealizacao is not null)
            .WithMessage("Informe o horário da realização.");
    }
}
