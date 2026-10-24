using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.CriarCampanha;

/// <summary>Caso de uso de criação manual de campanha (RF-01) — sempre atrelada a um épico do usuário.</summary>
public sealed partial class CriarCampanhaUseCase(
    IEpicoRepository epicoRepository,
    ICampanhaRepository campanhaRepository,
    IUnitOfWork unitOfWork,
    ILogger<CriarCampanhaUseCase> logger)
{
    public async Task<CriarCampanhaResult> ExecuteAsync(CriarCampanhaCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var epico = await epicoRepository.BuscarPorIdAsync(command.EpicoId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new EpicoNaoEncontradoException();

        if (epico.Estado == EstadoEpico.Concluido)
        {
            LogAtribuicaoRecusadaEpicoConcluido(epico.Id);
            throw new EpicoConcluidoException();
        }

        var prazoInicio = command.PrazoInicio ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var prazoFim = command.PrazoFim ?? epico.PrazoFim;

        var campanha = new Campanha
        {
            EpicoId = epico.Id,
            Titulo = command.Titulo,
            Descricao = command.Descricao,
            AreaDaVida = epico.AreaDaVida,
            Dificuldade = command.Dificuldade,
            Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Dificuldade),
            PrazoInicio = prazoInicio,
            PrazoFim = prazoFim,
            ValorMensurado = command.ValorMensurado,
            Unidade = command.ValorMensurado is null ? null : command.Unidade,
            ValorAtual = command.ValorMensurado is null ? null : 0
        };

        await campanhaRepository.AdicionarAsync(campanha, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogCampanhaCriada(campanha.Id, epico.Id);

        return new CriarCampanhaResult(
            campanha.Id, campanha.EpicoId, campanha.Titulo, campanha.Descricao, campanha.AreaDaVida,
            campanha.Dificuldade, campanha.Pontuacao, campanha.PrazoInicio, campanha.PrazoFim,
            campanha.ValorMensurado, campanha.Unidade, campanha.ValorAtual, campanha.Estado);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de criar campanha para outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de atribuir campanha ao épico {EpicoId} já concluído rejeitada")]
    private partial void LogAtribuicaoRecusadaEpicoConcluido(long epicoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Campanha {CampanhaId} criada para o épico {EpicoId}")]
    private partial void LogCampanhaCriada(long campanhaId, long epicoId);
}
