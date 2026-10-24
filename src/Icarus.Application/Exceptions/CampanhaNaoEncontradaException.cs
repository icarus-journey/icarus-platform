namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando a campanha solicitada não existe, ou existe mas pertence a outro usuário
/// (RF-02.4) — mesmo racional de <see cref="PerfilNaoEncontradoException"/>: não diferencia
/// os dois casos. Também usada quando uma campanha referenciada (ex.: ao criar uma missão) não
/// é encontrada ou não pertence ao usuário.
/// </summary>
public sealed class CampanhaNaoEncontradaException() : Exception("Campanha não encontrada.");
