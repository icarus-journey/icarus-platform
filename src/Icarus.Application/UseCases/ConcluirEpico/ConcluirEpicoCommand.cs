namespace Icarus.Application.UseCases.ConcluirEpico;

public sealed record ConcluirEpicoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long EpicoId);
