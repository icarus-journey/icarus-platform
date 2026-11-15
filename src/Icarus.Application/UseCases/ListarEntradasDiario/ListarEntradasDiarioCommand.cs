namespace Icarus.Application.UseCases.ListarEntradasDiario;

public sealed record ListarEntradasDiarioCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado);
