using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.InativarMissao;

public sealed record InativarMissaoResult(long Id, StatusMissao Status, int OcorrenciasCanceladas);
