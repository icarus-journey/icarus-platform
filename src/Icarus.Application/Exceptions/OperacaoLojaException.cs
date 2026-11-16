namespace Icarus.Application.Exceptions;

/// <summary>Código público estável; mensagens técnicas nunca são enviadas ao aplicativo.</summary>
public sealed class OperacaoLojaException(string codigo, string mensagem, bool naoEncontrado = false)
    : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
    public bool NaoEncontrado { get; } = naoEncontrado;
}
