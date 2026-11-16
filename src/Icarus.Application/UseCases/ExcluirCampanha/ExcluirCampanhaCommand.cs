namespace Icarus.Application.UseCases.ExcluirCampanha;

public sealed record ExcluirCampanhaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long CampanhaId);
