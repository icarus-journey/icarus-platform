namespace Icarus.Application.Interfaces;

/// <summary>
/// Gera e verifica hash de senha. A implementação concreta (algoritmo,
/// parâmetros de custo) fica em Icarus.Infrastructure — o resto do código
/// depende só deste contrato.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Gera um novo hash a partir de uma senha em texto puro.</summary>
    string Hash(string senha);

    /// <summary>Verifica se a senha em texto puro corresponde ao hash informado.</summary>
    bool Verify(string senha, string hash);
}
