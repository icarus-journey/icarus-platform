namespace Icarus.Application.UseCases.ExcluirEpico;

public sealed record ExcluirEpicoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long EpicoId);
