namespace Icarus.Application.UseCases.ObterFotoPerfil;

public sealed record ObterFotoPerfilCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado);
