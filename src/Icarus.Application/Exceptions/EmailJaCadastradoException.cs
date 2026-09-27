namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada quando o e-mail informado no cadastro já pertence a outro usuário
/// (RF-01.1: e-mail único). Mapeada para uma resposta HTTP pela camada de API.
/// </summary>
public sealed class EmailJaCadastradoException(string email)
    : Exception($"O e-mail '{email}' já está cadastrado.")
{
    public string Email { get; } = email;
}
