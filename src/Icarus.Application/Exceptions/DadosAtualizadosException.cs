namespace Icarus.Application.Exceptions;

public sealed class DadosAtualizadosException(Exception origem)
    : Exception("Seus dados mudaram. Atualize a tela e tente novamente.", origem);
