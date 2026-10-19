using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarMissao;

/// <summary>Caso de uso de edição de missão (RF-01) — não altera a campanha dona nem o status.</summary>
public sealed partial class AtualizarMissaoUseCase(
    IMissaoRepository missaoRepository,
    IUnitOfWork unitOfWork,
    ILogger<AtualizarMissaoUseCase> logger)
{
    public async Task<AtualizarMissaoResult> ExecuteAsync(AtualizarMissaoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var missao = await missaoRepository.BuscarPorIdAsync(command.MissaoId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new MissaoNaoEncontradaException();

        if (missao.ConcluidaEm is not null)
        {
            LogEdicaoRejeitada(missao.Id);
            throw new MissaoConcluidaException();
        }

        var dataReferencia = command.DataLimite ?? DateOnly.FromDateTime(DateTime.UtcNow);

        missao.Titulo = command.Titulo;
        missao.Descricao = command.Descricao;
        missao.Beneficio = command.Beneficio;
        missao.Dificuldade = command.Dificuldade;
        missao.Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Dificuldade);
        missao.DataLimite = command.DataLimite;
        missao.Horario = ParaHorario(dataReferencia, command.Horario);

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogMissaoAtualizada(missao.Id);

        return new AtualizarMissaoResult(
            missao.Id, missao.CampanhaId, missao.Titulo, missao.Descricao, missao.Beneficio,
            missao.Dificuldade, missao.Pontuacao, missao.DataLimite, missao.Horario, missao.Status, missao.ConcluidaEm);
    }

    private static DateTimeOffset ParaHorario(DateOnly data, string? horario)
    {
        var hora = horario is not null && TimeOnly.TryParse(horario, out var parsed) ? parsed : new TimeOnly(9, 0);
        return new DateTimeOffset(data.ToDateTime(hora), TimeSpan.Zero);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de editar missão de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Missão {MissaoId} atualizada")]
    private partial void LogMissaoAtualizada(long missaoId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de editar missão {MissaoId} já concluída rejeitada")]
    private partial void LogEdicaoRejeitada(long missaoId);
}
