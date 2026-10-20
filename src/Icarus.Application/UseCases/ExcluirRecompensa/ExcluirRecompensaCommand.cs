namespace Icarus.Application.UseCases.ExcluirRecompensa;

public sealed record ExcluirRecompensaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long RecompensaId);
