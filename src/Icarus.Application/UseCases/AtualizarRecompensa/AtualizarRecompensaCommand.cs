namespace Icarus.Application.UseCases.AtualizarRecompensa;

public sealed record AtualizarRecompensaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long RecompensaId, string Titulo, string? Descricao, int Custo);
