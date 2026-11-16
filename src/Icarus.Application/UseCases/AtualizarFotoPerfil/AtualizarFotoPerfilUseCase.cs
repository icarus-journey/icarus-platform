using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarFotoPerfil;

/// <summary>
/// Caso de uso de trocar a foto de perfil do usuário. O objeto é guardado no MinIO sob a
/// chave do próprio usuário — enviar uma foto nova sempre sobrescreve a anterior.
/// </summary>
public sealed partial class AtualizarFotoPerfilUseCase(
    IUsuarioRepository usuarioRepository,
    IArmazenamentoDeFotoPerfil armazenamento,
    IUnitOfWork unitOfWork,
    ILogger<AtualizarFotoPerfilUseCase> logger)
{
    private static readonly HashSet<string> TiposAceitos = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png" };
    private const long TamanhoMaximoEmBytes = 5 * 1024 * 1024;

    public async Task ExecuteAsync(AtualizarFotoPerfilCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        if (!TiposAceitos.Contains(command.ContentType))
        {
            throw new FotoPerfilInvalidaException("Envie uma imagem JPEG ou PNG.");
        }

        if (command.Tamanho <= 0 || command.Tamanho > TamanhoMaximoEmBytes)
        {
            throw new FotoPerfilInvalidaException("A imagem deve ter no máximo 5 MB.");
        }

        var usuario = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();

        var chave = await armazenamento.SalvarAsync(
            usuario.Id.ToString(), command.Conteudo, command.ContentType, command.Tamanho, cancellationToken);

        usuario.FotoChave = chave;
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogFotoAtualizada(usuario.Id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de atualizar foto de perfil de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Foto de perfil do usuário {UsuarioId} atualizada")]
    private partial void LogFotoAtualizada(Guid usuarioId);
}
