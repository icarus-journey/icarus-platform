namespace Icarus.Application.UseCases.ResgatarRecompensa;

public sealed record ResgatarRecompensaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long? RecompensaId, Guid OperacaoId);
