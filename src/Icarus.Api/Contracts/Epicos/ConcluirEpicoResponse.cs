using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Epicos;

public sealed record ConcluirEpicoResponse(long Id, EstadoEpico Estado);
