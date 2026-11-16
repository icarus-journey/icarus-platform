namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando o refresh token não existe, expirou, já foi revogado, ou já foi
/// trocado antes (reuso — ver <see cref="Domain.Entities.TokenRenovacao.SubstituidoPor"/>).
/// Mensagem genérica deliberadamente: não diferencia os casos, mesmo racional do
/// login (não dar pista de qual deles aconteceu).
/// </summary>
public sealed class RefreshTokenInvalidoException() : Exception("Refresh token inválido ou expirado.");
