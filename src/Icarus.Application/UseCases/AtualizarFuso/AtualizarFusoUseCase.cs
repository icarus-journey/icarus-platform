using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarFuso;

public sealed partial class AtualizarFusoUseCase(
    IUsuarioRepository usuarioRepository,
    IUnitOfWork unitOfWork,
    ILogger<AtualizarFusoUseCase> logger)
{
    public async Task<AtualizarFusoResult> ExecuteAsync(AtualizarFusoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var usuario = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();

        if (usuario.FusoHorario != command.FusoHorario)
        {
            usuario.FusoHorario = command.FusoHorario;
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
            LogFusoAtualizado();
        }

        return new AtualizarFusoResult(usuario.Id, usuario.FusoHorario!);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de editar fuso de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Fuso horário do usuário atualizado")]
    private partial void LogFusoAtualizado();
}
