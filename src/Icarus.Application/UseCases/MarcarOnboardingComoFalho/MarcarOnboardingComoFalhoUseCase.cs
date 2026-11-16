using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.MarcarOnboardingComoFalho;

public sealed partial class MarcarOnboardingComoFalhoUseCase(
    ITrabalhoOnboardingRepository trabalhoRepository,
    IUnitOfWork unitOfWork,
    ILogger<MarcarOnboardingComoFalhoUseCase> logger)
{
    public async Task ExecuteAsync(MarcarOnboardingComoFalhoCommand command, CancellationToken cancellationToken)
    {
        var trabalho = await trabalhoRepository.BuscarPorIdAsync(command.JobId, cancellationToken);
        if (trabalho is null || trabalho.Status != StatusTrabalhoOnboarding.Pendente)
        {
            // Idempotente: nada a fazer se o job não existe ou já foi concluído/marcado antes.
            return;
        }

        trabalho.Status = StatusTrabalhoOnboarding.Falhou;
        trabalho.ErroMensagem = command.ErroMensagem;
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        LogOnboardingFalhou(command.JobId, command.ErroMensagem);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Onboarding {JobId} marcado como falho: {Erro}")]
    private partial void LogOnboardingFalhou(Guid jobId, string erro);
}
