namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando o e-mail não existe ou a senha não confere no login (RF-02.1).
/// Deliberadamente não diferencia "e-mail não encontrado" de "senha errada" —
/// as duas situações usam a mesma exceção e a mesma mensagem genérica, para
/// não revelar a quem tenta logar se um e-mail está ou não cadastrado.
/// </summary>
public sealed class CredenciaisInvalidasException() : Exception("E-mail ou senha inválidos.");
