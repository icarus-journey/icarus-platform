namespace Icarus.Application.UseCases.ConsultarJornada;

public sealed record ConsultarJornadaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado);
