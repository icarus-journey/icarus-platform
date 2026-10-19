using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarEpico;

/// <summary>Caso de uso de edição de épico (RF-01) — não altera área da vida nem estado.</summary>
public sealed partial class AtualizarEpicoUseCase(
    IEpicoRepository epicoRepository,
    IUnitOfWork unitOfWork,
    ILogger<AtualizarEpicoUseCase> logger)
{
    public async Task<AtualizarEpicoResult> ExecuteAsync(AtualizarEpicoCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var epico = await epicoRepository.BuscarPorIdAsync(command.EpicoId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new EpicoNaoEncontradoException();

        epico.Titulo = command.Titulo;
        epico.Descricao = command.Descricao;
        epico.Dificuldade = command.Dificuldade;
        epico.Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Dificuldade);
        epico.PrazoInicio = command.PrazoInicio;
        epico.PrazoFim = command.PrazoFim;

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogEpicoAtualizado(epico.Id);

        return new AtualizarEpicoResult(
            epico.Id, epico.Titulo, epico.Descricao, epico.AreaDaVida, epico.Dificuldade,
            epico.Pontuacao, epico.PrazoInicio, epico.PrazoFim, epico.Estado);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de editar épico de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Épico {EpicoId} atualizado")]
    private partial void LogEpicoAtualizado(long epicoId);
}
