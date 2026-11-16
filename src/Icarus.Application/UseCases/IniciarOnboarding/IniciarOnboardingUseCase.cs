using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.IniciarOnboarding;

/// <summary>
/// Caso de uso que inicia o onboarding por IA (RF-01.5/RF-13): cria o trabalho de rastreio e
/// publica o job na fila para o worker gerar épico+campanha+missões de forma assíncrona.
/// </summary>
public sealed partial class IniciarOnboardingUseCase(
    ITrabalhoOnboardingRepository trabalhoRepository,
    IUnitOfWork unitOfWork,
    IOnboardingJobPublisher publisher,
    ILogger<IniciarOnboardingUseCase> logger)
{
    public async Task<IniciarOnboardingResult> ExecuteAsync(IniciarOnboardingCommand command, CancellationToken cancellationToken)
    {
        // RF-02.4: mesmo racional de ObterPerfil/AtualizarRotina — id de outro usuário é
        // tratado como recurso inexistente, nunca revelado.
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var trabalho = new TrabalhoOnboarding
        {
            Id = Guid.NewGuid(),
            UsuarioId = command.UsuarioIdSolicitado,
            Status = StatusTrabalhoOnboarding.Pendente
        };

        await trabalhoRepository.AdicionarAsync(trabalho, cancellationToken);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        // Publica só depois de persistir o trabalho — se a fila falhar aqui, o chamador recebe
        // o erro e pode tentar de novo; se persistisse depois, uma falha deixaria uma mensagem
        // em voo sem nenhum registro de rastreio.
        await publisher.PublicarAsync(trabalho.Id, trabalho.UsuarioId, command.Respostas, cancellationToken);

        LogOnboardingIniciado(trabalho.Id);

        return new IniciarOnboardingResult(trabalho.Id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de iniciar onboarding de outro usuário rejeitada")]
    private partial void LogAcessoNegado();

    [LoggerMessage(Level = LogLevel.Information, Message = "Onboarding {JobId} iniciado e publicado na fila")]
    private partial void LogOnboardingIniciado(Guid jobId);
}
