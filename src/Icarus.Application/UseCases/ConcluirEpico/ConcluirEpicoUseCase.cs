using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ConcluirEpico;

/// <summary>
/// Caso de uso de conclusão manual de épico (RF-03.8). Conclusão é irreversível e não muda o
/// estado das campanhas do épico — elas permanecem como estavam, mesmo que ainda abertas.
/// Idempotente: concluir de novo um épico já concluído é um no-op.
/// </summary>
public sealed partial class ConcluirEpicoUseCase(
    IEpicoRepository epicoRepository,
    IUnitOfWork unitOfWork,
    ILogger<ConcluirEpicoUseCase> logger)
{
    public async Task<ConcluirEpicoResult> ExecuteAsync(ConcluirEpicoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var epico = await epicoRepository.BuscarPorIdAsync(command.EpicoId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new EpicoNaoEncontradoException();
        if (epico.Estado == EstadoEpico.Excluido) throw new EpicoNaoEncontradoException();

        if (epico.Estado != EstadoEpico.Concluido)
        {
            epico.Estado = EstadoEpico.Concluido;
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
            LogEpicoConcluido(epico.Id);
        }

        return new ConcluirEpicoResult(epico.Id, epico.Estado);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de concluir épico de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Épico {EpicoId} concluído")]
    private partial void LogEpicoConcluido(long epicoId);
}
