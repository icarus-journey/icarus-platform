using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.CriarMissao;

/// <summary>Caso de uso de criação manual de missão (RF-01), com ou sem campanha.</summary>
public sealed partial class CriarMissaoUseCase(
    ICampanhaRepository campanhaRepository,
    IMissaoRepository missaoRepository,
    IUnitOfWork unitOfWork,
    ILogger<CriarMissaoUseCase> logger)
{
    public async Task<CriarMissaoResult> ExecuteAsync(CriarMissaoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        if (command.CampanhaId is { } campanhaId)
        {
            _ = await campanhaRepository.BuscarPorIdAsync(campanhaId, command.UsuarioIdSolicitado, cancellationToken)
                ?? throw new CampanhaNaoEncontradaException();
        }

        var dataReferencia = command.DataLimite ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var missao = new Missao
        {
            UsuarioId = command.UsuarioIdSolicitado,
            CampanhaId = command.CampanhaId,
            Titulo = command.Titulo,
            Descricao = command.Descricao,
            Beneficio = command.Beneficio,
            Dificuldade = command.Dificuldade,
            Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Dificuldade),
            DataLimite = command.DataLimite,
            Horario = ParaHorario(dataReferencia, command.Horario)
        };

        await missaoRepository.AdicionarAsync(missao, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogMissaoCriada(missao.Id);

        return new CriarMissaoResult(
            missao.Id, missao.CampanhaId, missao.Titulo, missao.Descricao, missao.Beneficio,
            missao.Dificuldade, missao.Pontuacao, missao.DataLimite, missao.Horario, missao.Status);
    }

    private static DateTimeOffset ParaHorario(DateOnly data, string? horario)
    {
        var hora = horario is not null && TimeOnly.TryParse(horario, out var parsed) ? parsed : new TimeOnly(9, 0);
        return new DateTimeOffset(data.ToDateTime(hora), TimeSpan.Zero);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de criar missão para outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Missão {MissaoId} criada")]
    private partial void LogMissaoCriada(long missaoId);
}
