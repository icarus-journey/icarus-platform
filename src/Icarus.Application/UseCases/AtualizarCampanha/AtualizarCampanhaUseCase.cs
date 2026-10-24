using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarCampanha;

/// <summary>Caso de uso de edição de campanha (RF-01) — não altera área da vida, estado nem o épico dono.</summary>
public sealed partial class AtualizarCampanhaUseCase(
    ICampanhaRepository campanhaRepository,
    IUnitOfWork unitOfWork,
    ILogger<AtualizarCampanhaUseCase> logger)
{
    public async Task<AtualizarCampanhaResult> ExecuteAsync(AtualizarCampanhaCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var campanha = await campanhaRepository.BuscarPorIdAsync(command.CampanhaId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new CampanhaNaoEncontradaException();

        if (campanha.Estado == EstadoCampanha.Concluida)
        {
            LogEdicaoRejeitada(campanha.Id);
            throw new CampanhaConcluidaException();
        }

        campanha.Titulo = command.Titulo;
        campanha.Descricao = command.Descricao;
        campanha.Dificuldade = command.Dificuldade;
        campanha.Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Dificuldade);
        campanha.PrazoInicio = command.PrazoInicio;
        campanha.PrazoFim = command.PrazoFim;
        campanha.ValorMensurado = command.ValorMensurado;
        if (command.ValorMensurado is null)
        {
            campanha.Unidade = null;
            campanha.ValorAtual = null;
        }
        else
        {
            campanha.Unidade = command.Unidade;
            campanha.ValorAtual ??= 0;
        }

        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogCampanhaAtualizada(campanha.Id);

        return new AtualizarCampanhaResult(
            campanha.Id, campanha.EpicoId, campanha.Titulo, campanha.Descricao, campanha.AreaDaVida,
            campanha.Dificuldade, campanha.Pontuacao, campanha.PrazoInicio, campanha.PrazoFim,
            campanha.ValorMensurado, campanha.Unidade, campanha.ValorAtual, campanha.Estado);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de editar campanha de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Campanha {CampanhaId} atualizada")]
    private partial void LogCampanhaAtualizada(long campanhaId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de editar campanha {CampanhaId} já concluída rejeitada")]
    private partial void LogEdicaoRejeitada(long campanhaId);
}
