using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ObterFotoPerfil;

public sealed partial class ObterFotoPerfilUseCase(
    IUsuarioRepository usuarioRepository,
    IArmazenamentoDeFotoPerfil armazenamento,
    ILogger<ObterFotoPerfilUseCase> logger)
{
    public async Task<ObterFotoPerfilResult> ExecuteAsync(ObterFotoPerfilCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var usuario = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();

        if (usuario.FotoChave is null)
        {
            throw new FotoPerfilNaoEncontradaException();
        }

        var (conteudo, contentType) = await armazenamento.ObterAsync(usuario.FotoChave, cancellationToken);
        return new ObterFotoPerfilResult(conteudo, contentType);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de buscar foto de perfil de outro usuário rejeitada")]
    private partial void LogAcessoNegado();
}
