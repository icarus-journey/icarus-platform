namespace Icarus.Application.UseCases.UtilizarRecompensa;

public sealed record UtilizarRecompensaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long ResgateId);
