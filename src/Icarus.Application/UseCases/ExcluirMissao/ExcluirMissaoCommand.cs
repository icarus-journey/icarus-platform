namespace Icarus.Application.UseCases.ExcluirMissao;

public sealed record ExcluirMissaoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long MissaoId);
