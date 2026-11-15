using Icarus.Application.Common;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Publica o job de onboarding na fila de mensageria para o worker de IA (icarus-ai) consumir.
/// A implementação concreta (RabbitMQ.Client) fica em Icarus.Infrastructure.
/// </summary>
public interface IOnboardingJobPublisher
{
    Task PublicarAsync(
        Guid jobId,
        Guid usuarioId,
        IReadOnlyList<RespostaOnboarding> respostas,
        CancellationToken cancellationToken);
}
