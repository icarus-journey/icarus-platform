using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ConcluirMissao;

/// <summary>
/// Caso de uso de conclusão de missão (RF-04.6). Cobre hoje só a missão não recorrente — a
/// única linha de <see cref="Ocorrencia"/> dela é criada aqui mesmo, na primeira conclusão
/// (DER seção 12: "a não recorrente terá uma única linha"). Geração de janela de ocorrências
/// para missão recorrente (RF-04.5) é outra funcionalidade, ainda não implementada.
///
/// Idempotente: concluir de novo uma missão já concluída não credita pontos nem gera uma
/// segunda movimentação — apenas devolve o resultado já registrado (reentrega de rede, duplo
/// toque). Editar uma missão concluída, por outro lado, é rejeitado (RF-04.3) em
/// AtualizarMissaoUseCase, não aqui.
/// </summary>
public sealed partial class ConcluirMissaoUseCase(
    IMissaoRepository missaoRepository,
    IOcorrenciaRepository ocorrenciaRepository,
    IMovimentacaoPontosRepository movimentacaoRepository,
    ICampanhaRepository campanhaRepository,
    IUsuarioRepository usuarioRepository,
    IUnitOfWork unitOfWork,
    ILogger<ConcluirMissaoUseCase> logger)
{
    public async Task<ConcluirMissaoResult> ExecuteAsync(ConcluirMissaoCommand command, CancellationToken cancellationToken)
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
            LogConclusaoDuplicada(missao.Id);
            var usuarioAtual = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken);
            var ocorrenciaAtual = await ocorrenciaRepository.BuscarPorMissaoEDataAsync(
                missao.Id, missao.DataLimite ?? DateOnly.FromDateTime(missao.ConcluidaEm.Value.UtcDateTime), cancellationToken);
            return new ConcluirMissaoResult(
                missao.Id, missao.ConcluidaEm.Value, ocorrenciaAtual?.Status == StatusOcorrencia.ConcluidaComAtraso,
                missao.Pontuacao, usuarioAtual?.SaldoPontos ?? 0);
        }

        var agora = DateTimeOffset.UtcNow;
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);
        var dataPrevista = missao.DataLimite ?? hoje;
        var concluidaComAtraso = missao.DataLimite is not null && missao.DataLimite < hoje;
        var statusOcorrencia = concluidaComAtraso ? StatusOcorrencia.ConcluidaComAtraso : StatusOcorrencia.Concluida;

        var ocorrencia = await ocorrenciaRepository.BuscarPorMissaoEDataAsync(missao.Id, dataPrevista, cancellationToken);
        if (ocorrencia is null)
        {
            ocorrencia = new Ocorrencia { MissaoId = missao.Id, DataPrevista = dataPrevista };
            await ocorrenciaRepository.AdicionarAsync(ocorrencia, cancellationToken);
        }

        ocorrencia.Status = statusOcorrencia;
        ocorrencia.RealizadaEm = agora;
        ocorrencia.ConcluidaEm = agora;

        missao.ConcluidaEm = agora;

        await movimentacaoRepository.AdicionarAsync(new MovimentacaoPontos
        {
            UsuarioId = missao.UsuarioId,
            Ocorrencia = ocorrencia,
            Tipo = TipoMovimentacaoPontos.GanhoMissao,
            Quantidade = missao.Pontuacao,
            Descricao = $"Conclusão da missão \"{missao.Titulo}\"",
            CriadoEm = agora
        }, cancellationToken);

        var usuario = await usuarioRepository.BuscarPorIdAsync(missao.UsuarioId, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();
        usuario.SaldoPontos += missao.Pontuacao;

        if (missao.CampanhaId is { } campanhaId)
        {
            var campanha = await campanhaRepository.BuscarPorIdAsync(campanhaId, missao.UsuarioId, cancellationToken);
            if (campanha is not null && campanha.ValorAtual is not null)
            {
                campanha.ValorAtual += missao.Pontuacao;
            }
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogMissaoConcluida(missao.Id, missao.Pontuacao);

        return new ConcluirMissaoResult(missao.Id, agora, concluidaComAtraso, missao.Pontuacao, usuario.SaldoPontos);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de concluir missão de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Missão {MissaoId} já estava concluída — pedido ignorado (idempotência)")]
    private partial void LogConclusaoDuplicada(long missaoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Missão {MissaoId} concluída, {Pontuacao} pontos creditados")]
    private partial void LogMissaoConcluida(long missaoId, int pontuacao);
}
