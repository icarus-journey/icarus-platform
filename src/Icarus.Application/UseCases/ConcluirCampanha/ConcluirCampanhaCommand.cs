namespace Icarus.Application.UseCases.ConcluirCampanha;

public sealed record ConcluirCampanhaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long CampanhaId);
