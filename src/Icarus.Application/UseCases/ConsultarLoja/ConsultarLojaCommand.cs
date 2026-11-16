namespace Icarus.Application.UseCases.ConsultarLoja;

public sealed record ConsultarLojaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado);
