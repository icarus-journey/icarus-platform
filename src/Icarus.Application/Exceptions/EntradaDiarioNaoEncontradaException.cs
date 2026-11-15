namespace Icarus.Application.Exceptions;

public sealed class EntradaDiarioNaoEncontradaException : Exception
{
    public EntradaDiarioNaoEncontradaException() : base("Entrada de diário não encontrada.") { }
}
