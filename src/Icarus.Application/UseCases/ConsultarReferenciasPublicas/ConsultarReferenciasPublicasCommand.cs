namespace Icarus.Application.UseCases.ConsultarReferenciasPublicas;

public sealed record ConsultarReferenciasPublicasCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado,
    string? AreaDaVida = null, string TipoDia = "todos");
