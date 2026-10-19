namespace Icarus.Application.UseCases.IniciarOnboarding;

/// <summary>Saída do caso de uso que inicia o onboarding — o id do trabalho (= job_id na fila).</summary>
public sealed record IniciarOnboardingResult(Guid JobId);
