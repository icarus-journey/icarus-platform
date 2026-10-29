namespace Icarus.Application.Exceptions;

public sealed class ProgressoCampanhaException(string codigo, string mensagem) : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
}
