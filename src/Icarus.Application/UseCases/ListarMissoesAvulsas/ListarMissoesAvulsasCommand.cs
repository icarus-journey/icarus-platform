namespace Icarus.Application.UseCases.ListarMissoesAvulsas;

public sealed record ListarMissoesAvulsasCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado);
