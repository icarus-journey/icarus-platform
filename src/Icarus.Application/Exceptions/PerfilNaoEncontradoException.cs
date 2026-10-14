namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando o perfil solicitado não existe, ou existe mas pertence a outro
/// usuário (RF-02.4). Deliberadamente não diferencia os dois casos — mesma mensagem
/// e status para ambos, para não confirmar a existência de um id que não é o do
/// chamador (mesmo racional do login/refresh: nunca revelar mais do que o necessário).
/// </summary>
public sealed class PerfilNaoEncontradoException() : Exception("Perfil não encontrado.");
