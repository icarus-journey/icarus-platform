using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ConcluirEpico;

public sealed record ConcluirEpicoResult(long Id, EstadoEpico Estado);
