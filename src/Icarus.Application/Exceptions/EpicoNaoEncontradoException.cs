namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando o épico solicitado não existe, ou existe mas pertence a outro usuário
/// (RF-02.4) — mesmo racional de <see cref="PerfilNaoEncontradoException"/>: não diferencia
/// os dois casos. Também usada quando um épico referenciado (ex.: ao criar uma campanha) não
/// é encontrado ou não pertence ao usuário.
/// </summary>
public sealed class EpicoNaoEncontradoException() : Exception("Épico não encontrado.");
