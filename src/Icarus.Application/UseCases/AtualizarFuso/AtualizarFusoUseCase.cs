using Icarus.Application.Exceptions;
using Icarus.Application.Common;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarFuso;

public sealed partial class AtualizarFusoUseCase(
    IUsuarioRepository usuarioRepository,
    IUnitOfWork unitOfWork,
    ILogger<AtualizarFusoUseCase> logger, ITransacaoConclusaoMissao? transacao = null, IProtecaoSequencia? protecoes = null, TimeProvider? relogio = null)
{
    public async Task<AtualizarFusoResult> ExecuteAsync(AtualizarFusoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        return transacao is null ? await AtualizarAsync(command, cancellationToken)
            : await transacao.ExecutarAsync(command.UsuarioIdSolicitado, ct => AtualizarAsync(command, ct), cancellationToken);
    }

    private async Task<AtualizarFusoResult> AtualizarAsync(AtualizarFusoCommand command, CancellationToken cancellationToken)
    {
        var usuario = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();

        if (usuario.FusoHorario != command.FusoHorario)
        {
            if (protecoes is not null) await protecoes.ReconciliarAsync(usuario, cancellationToken);
            if (!FusoHorario.EhValido(usuario.FusoHorario))
            {
                usuario.ProtecaoAtivadaEm = (relogio ?? TimeProvider.System).GetUtcNow();
                usuario.FusoInicioProtecao = command.FusoHorario;
            }
            usuario.FusoHorario = command.FusoHorario;
            protecoes?.SolicitarRecalculo(usuario.Id);
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
