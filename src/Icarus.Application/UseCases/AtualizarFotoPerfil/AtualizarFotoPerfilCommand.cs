namespace Icarus.Application.UseCases.AtualizarFotoPerfil;

public sealed record AtualizarFotoPerfilCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    Stream Conteudo,
    string ContentType,
    long Tamanho);
