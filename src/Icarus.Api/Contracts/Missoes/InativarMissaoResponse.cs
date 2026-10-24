using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

public sealed record InativarMissaoResponse(long Id, StatusMissao Status, int OcorrenciasCanceladas);
