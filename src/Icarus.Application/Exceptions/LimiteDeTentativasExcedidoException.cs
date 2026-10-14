namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando a combinação de e-mail normalizado + origem (RF-02.3) acumulou 5
/// falhas de login em 15 minutos. Mensagem genérica — não confirma se a conta existe.
/// </summary>
public sealed class LimiteDeTentativasExcedidoException() : Exception("Muitas tentativas de login. Tente novamente mais tarde.");
