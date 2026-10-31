using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ConcluirMissao;

/// <summary>
/// Caso de uso de conclusão de missão (RF-04.6), para missão recorrente ou não. A ocorrência a
/// concluir é resolvida por <see cref="IOcorrenciaRepository.BuscarParaConcluirAsync"/>: a de
/// hoje, senão a pendente/atrasada mais antiga (recupera um dia perdido). Normalmente ela já
/// existe — gerada na criação da missão ou pela manutenção diária da janela
/// (<c>EstenderJanelaDeOcorrenciasUseCase</c>); se não existir (caso defensivo, ex.: missão
/// criada antes desta funcionalidade), cria uma pontualmente para hoje.
///
/// <see cref="Missao.ConcluidaEm"/> só é espelhado para missão **não recorrente** (DER seção
/// 9) — para recorrente, a missão continua ativa entre uma conclusão e outra; é imutável
/// (RF-04.3) só quando preenchido, então <c>AtualizarMissaoUseCase</c> nunca bloqueia edição de
/// missão recorrente por esta via.
///
/// Idempotente: concluir de novo a ocorrência de hoje (ou a única, pra não recorrente) não
/// credita pontos nem gera uma segunda movimentação — apenas devolve o resultado já registrado.
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
        if (missao.Status == StatusMissao.Excluida) throw new MissaoNaoEncontradaException();

        if (missao.Status == StatusMissao.Inativa)
        {
            LogConclusaoRecusadaInativa(missao.Id);
            throw new MissaoInativaException();
        }

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var ehRecorrente = missao.Recorrencia is not null;

        var ocorrencia = await ocorrenciaRepository.BuscarParaConcluirAsync(missao.Id, hoje, cancellationToken);

        if (ocorrencia is not null && ocorrencia.Status is StatusOcorrencia.Concluida or StatusOcorrencia.ConcluidaComAtraso)
        {
            LogConclusaoDuplicada(missao.Id, ocorrencia.Id);
            var usuarioAtual = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken);
            return new ConcluirMissaoResult(
                missao.Id, ocorrencia.ConcluidaEm!.Value, ocorrencia.Status == StatusOcorrencia.ConcluidaComAtraso,
                missao.Pontuacao, usuarioAtual?.SaldoPontos ?? 0);
        }

        Campanha? campanhaVinculada = null;
        if (missao.CampanhaId is { } idCampanha)
        {
            campanhaVinculada = await campanhaRepository.BuscarPorIdAsync(idCampanha, missao.UsuarioId, cancellationToken)
                ?? throw new CampanhaNaoEncontradaException();
            if (campanhaVinculada.Estado == EstadoCampanha.Excluida) throw new CampanhaNaoEncontradaException();
            if (campanhaVinculada.Estado == EstadoCampanha.Concluida) throw new CampanhaConcluidaException();
            campanhaVinculada.AtualizadoEm = DateTimeOffset.UtcNow;
        }
        var agora = DateTimeOffset.UtcNow;
        missao.AtualizadoEm = agora;

        if (ocorrencia is null)
        {
            // Defensivo: não deveria faltar (gerada na criação ou pela manutenção diária), mas
            // cobre missão criada antes desta funcionalidade existir. Usa a data limite da
            // missão (quando houver) para que o atraso seja calculado corretamente.
            ocorrencia = new Ocorrencia { MissaoId = missao.Id, DataPrevista = missao.DataLimite ?? hoje };
            await ocorrenciaRepository.AdicionarAsync(ocorrencia, cancellationToken);
        }

        var concluidaComAtraso = ocorrencia.DataPrevista < hoje;
        ocorrencia.Status = concluidaComAtraso ? StatusOcorrencia.ConcluidaComAtraso : StatusOcorrencia.Concluida;
        ocorrencia.RealizadaEm = agora;
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
            Quantidade = missao.Pontuacao,
            Descricao = $"Conclusão da missão \"{missao.Titulo}\"",
            CriadoEm = agora
        }, cancellationToken);

        var usuario = await usuarioRepository.BuscarPorIdAsync(missao.UsuarioId, cancellationToken)
            ?? throw new PerfilNaoEncontradoException();
        usuario.SaldoPontos += missao.Pontuacao;

        if (campanhaVinculada is { } campanha)
        {
            // Somente o contador de gamificação usa pontos; medidas de resultado são manuais.
            if (campanha is not null && campanha.ValorAtual is not null
                && string.Equals(campanha.Unidade, "pontos", StringComparison.OrdinalIgnoreCase))
            {
                campanha.ValorAtual += missao.Pontuacao;
            }
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogMissaoConcluida(missao.Id, ocorrencia.Id, missao.Pontuacao);

        return new ConcluirMissaoResult(missao.Id, agora, concluidaComAtraso, missao.Pontuacao, usuario.SaldoPontos);
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
