using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.CriarEpico;

/// <summary>Caso de uso de criação manual de épico (RF-01, sem passar pelo onboarding por IA).</summary>
public sealed partial class CriarEpicoUseCase(
    IEpicoRepository epicoRepository,
    IUnitOfWork unitOfWork,
    ILogger<CriarEpicoUseCase> logger)
{
    public async Task<CriarEpicoResult> ExecuteAsync(CriarEpicoCommand command, CancellationToken cancellationToken)
    {
        // RF-02.4: mesmo racional de ObterPerfil/IniciarOnboarding — id de outro usuário é
        // tratado como recurso inexistente, nunca revelado.
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var prazoInicio = command.PrazoInicio ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var prazoFim = command.PrazoFim ?? prazoInicio.AddYears(1);

        var epico = new Epico
        {
            UsuarioId = command.UsuarioIdSolicitado,
            Titulo = command.Titulo,
            Descricao = command.Descricao,
            AreaDaVida = command.AreaDaVida,
            Dificuldade = command.Dificuldade,
            Pontuacao = MapeadorPontuacao.ParaPontuacao(command.Dificuldade),
            PrazoInicio = prazoInicio,
            PrazoFim = prazoFim
        };

        await epicoRepository.AdicionarAsync(epico, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogEpicoCriado(epico.Id);

        return new CriarEpicoResult(
            epico.Id, epico.Titulo, epico.Descricao, epico.AreaDaVida, epico.Dificuldade,
            epico.Pontuacao, epico.PrazoInicio, epico.PrazoFim, epico.Estado);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de criar épico para outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Épico {EpicoId} criado manualmente")]
    private partial void LogEpicoCriado(long epicoId);
}
