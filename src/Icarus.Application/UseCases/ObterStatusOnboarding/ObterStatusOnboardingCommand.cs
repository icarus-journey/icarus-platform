namespace Icarus.Application.UseCases.ObterStatusOnboarding;

/// <summary>
/// Entrada do caso de uso de consultar o status de um trabalho de onboarding (RF-02.4
/// aplicado ao job, igual a ObterPerfil/AtualizarRotina).
/// </summary>
public sealed record ObterStatusOnboardingCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, Guid JobId);
