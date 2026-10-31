namespace Icarus.Application.UseCases.ConcluirMissao;

public sealed record ConcluirMissaoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long MissaoId, long? OcorrenciaId = null);
