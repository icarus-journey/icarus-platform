namespace Icarus.Application.UseCases.InativarMissao;

public sealed record InativarMissaoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long MissaoId);
