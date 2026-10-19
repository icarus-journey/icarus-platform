using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Campanhas;

public sealed record ConcluirCampanhaResponse(long Id, EstadoCampanha Estado);
