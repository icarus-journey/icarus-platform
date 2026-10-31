using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ConcluirMissao;

/// <summary>Conclui uma execução elegível no dia local do titular: hoje, senão o atraso mais
/// antigo. Nunca antecipa, recria canceladas ou conclui campanha planejada. Repetição da
/// execução concluída devolve seu crédito original, sem alterar saldo.</summary>
public sealed partial class ConcluirMissaoUseCase(
    IMissaoRepository missaoRepository,
    IOcorrenciaRepository ocorrenciaRepository,
    IMovimentacaoPontosRepository movimentacaoRepository,
    ICampanhaRepository campanhaRepository,
    IUsuarioRepository usuarioRepository,
    IUnitOfWork unitOfWork,
    ILogger<ConcluirMissaoUseCase> logger, TimeProvider? relogio = null, IIniciadorCampanhas? iniciador = null)
{
    public async Task<ConcluirMissaoResult> ExecuteAsync(ConcluirMissaoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        if (iniciador is not null) await iniciador.ExecuteAsync(command.UsuarioIdSolicitado, cancellationToken);
        var missao = await missaoRepository.BuscarPorIdAsync(command.MissaoId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new MissaoNaoEncontradaException();
        if (missao.Status == StatusMissao.Excluida) throw new MissaoNaoEncontradaException();
        var usuario = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();
        missao.Usuario = usuario;
        Campanha? campanhaVinculada = null;
        if (missao.CampanhaId is { } idCampanha)
        {
            campanhaVinculada = await campanhaRepository.BuscarPorIdAsync(idCampanha, missao.UsuarioId, cancellationToken)
                ?? throw new CampanhaNaoEncontradaException();
            missao.Campanha = campanhaVinculada;
            if (campanhaVinculada.Estado == EstadoCampanha.Excluida) throw new CampanhaNaoEncontradaException();
            if (campanhaVinculada.Estado == EstadoCampanha.Concluida) throw new CampanhaConcluidaException();
        }
        var agora = RealizacaoMissao.NaPrecisaoPersistida((relogio ?? TimeProvider.System).GetUtcNow());
        var hoje = FusoHorario.DataLocal(agora, usuario.FusoHorario);
        if (RegrasRecorrencia.EncerrarSeNecessario(missao, hoje))
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        if (missao.Status == StatusMissao.Inativa) throw new MissaoInativaException();
        Ocorrencia? escolhida = null;
        if (command.OcorrenciaId is { } id)
        {
            escolhida = missao.Ocorrencias.SingleOrDefault(o => o.Id == id) ?? throw new MissaoNaoEncontradaException();
            if (escolhida.DataPrevista > hoje) throw new EdicaoMissaoException("execucao_futura", "Esta execução ainda não está disponível.", true);
            if (!RegrasRecorrencia.DentroDosLimites(missao, escolhida.DataPrevista) || escolhida.Status == StatusOcorrencia.Excluida)
                throw new EdicaoMissaoException("ocorrencia_indisponivel", "Esta execução não está disponível para concluir.", true);
        }
        var disponibilidade = ExecucaoMissaoResult.DaMissao(missao, agora, selecionada: escolhida);
        if (disponibilidade.CodigoBloqueio is { } bloqueio)
            throw new EdicaoMissaoException(bloqueio,
                bloqueio == "campanha_nao_iniciada" ? "A campanha ainda não começou."
                : bloqueio == "execucao_futura" ? "Esta execução ainda não está disponível."
                : "Não há execução pendente disponível para concluir.", true);
        var ehRecorrente = missao.Recorrencia is not null;
        var ocorrencia = escolhida ?? RegrasRecorrencia.Selecionar(missao, hoje)
            ?? throw new EdicaoMissaoException("sem_execucao_pendente", "Não há execução pendente disponível para concluir.", true);
        if (command.DataPrevistaEsperada is { } prevista && ocorrencia.DataPrevista != prevista)
            throw new EdicaoMissaoException("execucao_alterada", "A execução mudou. Atualize os dados antes de concluir.", true);
        if (RegrasRecorrencia.Concluida(ocorrencia))
        {
            if (ocorrencia.ConcluidaEm is null)
                throw new EdicaoMissaoException("historico_incompleto", "Esta execução já está concluída e precisa ser revisada.", true);
            LogConclusaoDuplicada(missao.Id, ocorrencia.Id);
            return new ConcluirMissaoResult(missao.Id, ocorrencia.ConcluidaEm!.Value,
                ocorrencia.Status == StatusOcorrencia.ConcluidaComAtraso, disponibilidade.Pontuacao!.Value, usuario.SaldoPontos, true,
                ocorrencia.RealizadaEm, ocorrencia.FusoHorarioRealizacao, ocorrencia.Id, ocorrencia.DataPrevista);
        }
        var realizacao = RealizacaoMissao.Resolver(command.DataRealizacao, command.HorarioRealizacao, agora, usuario.FusoHorario);
        if (command.FusoHorarioEsperado is { } fuso && fuso != realizacao.FusoHorario)
            throw new EdicaoMissaoException("fuso_alterado", "O horário da conta mudou. Reabra a confirmação.", true);
        if (ocorrencia.Id == 0)
        {
            await ocorrenciaRepository.AdicionarAsync(ocorrencia, cancellationToken);
            if (!missao.Ocorrencias.Contains(ocorrencia)) missao.Ocorrencias.Add(ocorrencia);
        }
        missao.AtualizadoEm = agora;
        if (campanhaVinculada is not null) campanhaVinculada.AtualizadoEm = agora;

        var planejamento = ehRecorrente ? PlanejamentoOcorrencia.DaExecucao(missao, ocorrencia) : null;
        var pontuacao = planejamento?.Pontuacao ?? missao.Pontuacao;
        var titulo = planejamento?.Titulo ?? missao.Titulo;
        var concluidaComAtraso = ocorrencia.DataPrevista < FusoHorario.DataLocal(realizacao.Instante, realizacao.FusoHorario);
        ocorrencia.Status = concluidaComAtraso ? StatusOcorrencia.ConcluidaComAtraso : StatusOcorrencia.Concluida;
        ocorrencia.RealizadaEm = realizacao.Instante;
        ocorrencia.FusoHorarioRealizacao = realizacao.FusoHorario;
        ocorrencia.ConcluidaEm = agora;

        if (!ehRecorrente)
        {
            missao.ConcluidaEm = agora;
        }

        await movimentacaoRepository.AdicionarAsync(new MovimentacaoPontos
        {
            UsuarioId = missao.UsuarioId,
            Ocorrencia = ocorrencia,
            Tipo = TipoMovimentacaoPontos.GanhoMissao,
            Quantidade = pontuacao,
            Descricao = $"Conclusão da missão \"{titulo}\"",
            CriadoEm = agora
        }, cancellationToken);

        usuario.SaldoPontos += pontuacao;

        if (campanhaVinculada is { } campanha)
        {
            // Somente o contador de gamificação usa pontos; medidas de resultado são manuais.
            if (campanha is not null && campanha.ValorAtual is not null
                && string.Equals(campanha.Unidade, "pontos", StringComparison.OrdinalIgnoreCase))
            {
                campanha.ValorAtual += pontuacao;
            }
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogMissaoConcluida(missao.Id, ocorrencia.Id, pontuacao);

        return new ConcluirMissaoResult(missao.Id, agora, concluidaComAtraso, pontuacao, usuario.SaldoPontos, false,
            ocorrencia.RealizadaEm, ocorrencia.FusoHorarioRealizacao, ocorrencia.Id, ocorrencia.DataPrevista);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de concluir missão de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de concluir missão {MissaoId} inativa rejeitada")]
    private partial void LogConclusaoRecusadaInativa(long missaoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ocorrência {OcorrenciaId} da missão {MissaoId} já estava concluída — pedido ignorado (idempotência)")]
    private partial void LogConclusaoDuplicada(long missaoId, long ocorrenciaId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ocorrência {OcorrenciaId} da missão {MissaoId} concluída, {Pontuacao} pontos creditados")]
    private partial void LogMissaoConcluida(long missaoId, long ocorrenciaId, int pontuacao);
}
