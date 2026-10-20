using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarPerfil;

public sealed partial class AtualizarPerfilUseCase(
    IUsuarioRepository usuarioRepository,
    IUnitOfWork unitOfWork,
    ILogger<AtualizarPerfilUseCase> logger)
{
    public async Task<AtualizarPerfilResult> ExecuteAsync(AtualizarPerfilCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var usuario = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();

        usuario.Estuda = command.Estuda;
        usuario.CargaHorariaEstudoSemanal = command.CargaHorariaEstudoSemanal;
        usuario.Trabalha = command.Trabalha;
        usuario.CargaHorariaTrabalhoSemanal = command.CargaHorariaTrabalhoSemanal;

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        LogPerfilAtualizado();

        return new AtualizarPerfilResult(usuario.Id, usuario.Nome, usuario.Email, usuario.DataNascimento,
            usuario.Estuda, usuario.CargaHorariaEstudoSemanal, usuario.Trabalha,
            usuario.CargaHorariaTrabalhoSemanal, usuario.FusoHorario);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de editar perfil de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Dados analíticos do perfil atualizados")]
    private partial void LogPerfilAtualizado();
}
