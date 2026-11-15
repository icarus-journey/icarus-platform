namespace Icarus.Application.UseCases.ListarEpicos;

public sealed record ListarEpicosCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado);
