using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.InativarMissao;

/// <summary>
/// Caso de uso de parar uma missão recorrente (RF-04.6) — "ação manual" de inativação, ao
/// lado do fim da recorrência ou da campanha, tratados pela manutenção. Só missão recorrente
/// pode ser inativada; a não recorrente só conclui, no prazo ou com atraso.
///
/// Cancela as ocorrências pendentes ainda não vencidas (<see cref="Ocorrencia.DataPrevista"/>
/// de hoje em diante) — elas não serão mais geradas nem concluídas. As pendentes já vencidas
/// ficam de fora: são preservadas como histórico, mas <c>ConcluirMissaoUseCase</c> passa a
/// rejeitar sua conclusão (<see cref="MissaoInativaException"/>) depois que a missão está
/// inativa. Idempotente: inativar de novo uma missão já inativa não cancela nada outra vez.
/// </summary>
public sealed partial class InativarMissaoUseCase(
    IMissaoRepository missaoRepository,
    IOcorrenciaRepository ocorrenciaRepository,
    IUnitOfWork unitOfWork,
    ILogger<InativarMissaoUseCase> logger, TimeProvider? relogio = null)
{
    public async Task<InativarMissaoResult> ExecuteAsync(InativarMissaoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var missao = await missaoRepository.BuscarPorIdAsync(command.MissaoId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new MissaoNaoEncontradaException();
        if (missao.Status == StatusMissao.Excluida) throw new MissaoNaoEncontradaException();

        if (missao.Recorrencia is null)
        {
            LogRecusadaNaoRecorrente(missao.Id);
            throw new MissaoNaoRecorrenteException();
        }

        if (missao.Status == StatusMissao.Inativa)
        {
            LogJaInativa(missao.Id);
            return new InativarMissaoResult(missao.Id, missao.Status, 0);
        }

        missao.Status = StatusMissao.Inativa;

        var hoje = FusoHorario.DataLocal((relogio ?? TimeProvider.System).GetUtcNow(), missao.Usuario?.FusoHorario);
        var pendentesNaoVencidas = await ocorrenciaRepository.ListarPendentesNaoVencidasAsync(missao.Id, hoje, cancellationToken);
        foreach (var ocorrencia in pendentesNaoVencidas)
        {
            ocorrencia.Status = StatusOcorrencia.Excluida;
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogMissaoInativada(missao.Id, pendentesNaoVencidas.Count);

        return new InativarMissaoResult(missao.Id, missao.Status, pendentesNaoVencidas.Count);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de inativar missão de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de inativar missão {MissaoId} não recorrente rejeitada")]
    private partial void LogRecusadaNaoRecorrente(long missaoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Missão {MissaoId} já estava inativa — pedido ignorado (idempotência)")]
    private partial void LogJaInativa(long missaoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Missão {MissaoId} inativada, {OcorrenciasCanceladas} ocorrência(s) pendente(s) cancelada(s)")]
    private partial void LogMissaoInativada(long missaoId, int ocorrenciasCanceladas);
}
