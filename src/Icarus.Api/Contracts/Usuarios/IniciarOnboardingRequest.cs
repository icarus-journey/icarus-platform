namespace Icarus.Api.Contracts.Usuarios;

/// <summary>Respostas do questionário de onboarding (RF-01.5/RF-13), em texto legível.</summary>
public sealed record IniciarOnboardingRequest(List<RespostaOnboardingRequest> Respostas);

public sealed record RespostaOnboardingRequest(string Pergunta, List<string> Respostas);
