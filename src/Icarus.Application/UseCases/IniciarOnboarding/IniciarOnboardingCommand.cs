using Icarus.Application.Common;

namespace Icarus.Application.UseCases.IniciarOnboarding;

/// <summary>
/// Entrada do caso de uso que inicia o onboarding por IA. Contrato interno, desacoplado
/// do HTTP. <paramref name="UsuarioIdAutenticado"/> vem da claim do token;
/// <paramref name="UsuarioIdSolicitado"/> vem da rota (RF-02.4).
/// </summary>
public sealed record IniciarOnboardingCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    IReadOnlyList<RespostaOnboarding> Respostas);
