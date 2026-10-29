namespace Icarus.Application.UseCases.CriarRecompensa;

public sealed record CriarRecompensaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, string Titulo, string? Descricao, int Custo);
