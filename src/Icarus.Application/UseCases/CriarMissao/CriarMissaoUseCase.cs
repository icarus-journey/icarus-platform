using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.CriarMissao;

/// <summary>
/// Caso de uso de criação manual de missão (RF-01), com ou sem campanha, recorrente ou não.
/// Ao ativar (toda missão nasce ativa — RF-04.5/DER seção 12), a ocorrência correspondente já é
/// gerada: uma única, para não recorrente; a janela dos próximos 30 dias, para recorrente. A
/// rotina diária (<c>EstenderJanelaDeOcorrenciasUseCase</c>) mantém essa janela depois.
/// </summary>
public sealed partial class CriarMissaoUseCase(
    ICampanhaRepository campanhaRepository,
    IMissaoRepository missaoRepository,
    IRecorrenciaRepository recorrenciaRepository,
    IOcorrenciaRepository ocorrenciaRepository,
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
            var campanha = await campanhaRepository.BuscarPorIdAsync(campanhaId, command.UsuarioIdSolicitado, cancellationToken)
                ?? throw new CampanhaNaoEncontradaException();

            if (campanha.Estado == EstadoCampanha.Excluida) throw new CampanhaNaoEncontradaException();
            if (campanha.Estado == EstadoCampanha.Concluida)
            {
                LogAtribuicaoRecusadaCampanhaConcluida(campanhaId);
                throw new CampanhaConcluidaException();
            }
            // A versão da campanha impede criar planejamento durante um encerramento concorrente.
            campanha.AtualizadoEm = DateTimeOffset.UtcNow;
        }

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataReferencia = command.DataLimite ?? hoje;

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

        List<DateOnly> datasDeOcorrencia;

        if (command.Recorrencia is { } recorrenciaInfo)
        {
            var regra = GeradorDeDatasRecorrencia.ConstruirRegra(recorrenciaInfo.Tipo, recorrenciaInfo.DiasSemana, dataReferencia.Day);
            var recorrencia = new Recorrencia
            {
                Missao = missao,
                Tipo = recorrenciaInfo.Tipo.ToString().ToUpperInvariant(),
                DataInicio = hoje,
                DataFim = recorrenciaInfo.DataFim,
                Regra = regra
            };
            missao.Recorrencia = recorrencia;
            await recorrenciaRepository.AdicionarAsync(recorrencia, cancellationToken);

            var fimDaJanela = hoje.AddDays(GeradorDeDatasRecorrencia.DiasDaJanelaPadrao);
            if (recorrenciaInfo.DataFim is { } dataFim && dataFim < fimDaJanela)
            {
                fimDaJanela = dataFim;
            }

            datasDeOcorrencia = GeradorDeDatasRecorrencia.Gerar(hoje, fimDaJanela, recorrenciaInfo.Tipo, regra);
        }
        else
        {
            datasDeOcorrencia = [dataReferencia];
        }

        foreach (var data in datasDeOcorrencia)
        {
            await ocorrenciaRepository.AdicionarAsync(new Ocorrencia
            {
                Missao = missao,
                DataPrevista = data,
                Planejamento = command.Recorrencia is null ? null : PlanejamentoOcorrencia.Capturar(missao)
            }, cancellationToken);
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogMissaoCriada(missao.Id, datasDeOcorrencia.Count);

        return new CriarMissaoResult(
            missao.Id, missao.CampanhaId, missao.Titulo, missao.Descricao, missao.Beneficio,
            missao.Dificuldade, missao.Pontuacao, missao.DataLimite, missao.Horario, missao.Status,
            missao.ConcluidaEm, command.Recorrencia is not null);
    }

    private static DateTimeOffset ParaHorario(DateOnly data, string? horario)
    {
        var hora = horario is not null && TimeOnly.TryParse(horario, out var parsed) ? parsed : new TimeOnly(9, 0);
        return new DateTimeOffset(data.ToDateTime(hora), TimeSpan.Zero);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de criar missão para outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de atribuir missão à campanha {CampanhaId} já concluída rejeitada")]
    private partial void LogAtribuicaoRecusadaCampanhaConcluida(long campanhaId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Missão {MissaoId} criada com {QuantidadeOcorrencias} ocorrência(s)")]
    private partial void LogMissaoCriada(long missaoId, int quantidadeOcorrencias);
}
