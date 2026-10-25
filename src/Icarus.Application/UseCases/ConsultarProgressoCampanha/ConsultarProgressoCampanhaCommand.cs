namespace Icarus.Application.UseCases.ConsultarProgressoCampanha;

public sealed record ConsultarProgressoCampanhaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long CampanhaId);
