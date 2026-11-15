namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando a missão solicitada não existe, ou existe mas pertence a outro usuário
/// (RF-02.4) — mesmo racional de <see cref="PerfilNaoEncontradoException"/>: não diferencia
/// os dois casos.
/// </summary>
public sealed class MissaoNaoEncontradaException() : Exception("Missão não encontrada.");
