namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando o trabalho de onboarding solicitado não existe, ou existe mas pertence a
/// outro usuário (RF-02.4) — mesmo racional de <see cref="PerfilNaoEncontradoException"/>:
/// não diferencia os dois casos, pra não confirmar a existência de um job que não é do
/// chamador.
/// </summary>
public sealed class TrabalhoOnboardingNaoEncontradoException() : Exception("Trabalho de onboarding não encontrado.");
