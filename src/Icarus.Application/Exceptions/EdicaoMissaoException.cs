namespace Icarus.Application.Exceptions;

public sealed class EdicaoMissaoException(string codigo, string mensagem, bool conflito = false) : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
    public bool Conflito { get; } = conflito;
}
