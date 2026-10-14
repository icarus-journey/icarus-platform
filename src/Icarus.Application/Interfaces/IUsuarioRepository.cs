using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="Usuario"/>. A implementação concreta (EF Core)
/// fica em Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Usado para checar duplicidade antes de cadastrar (RF-01.1: e-mail único).</summary>
    Task<bool> ExisteComEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Usado no login (RF-02.1) para validar a senha. Nulo se o e-mail não existir.</summary>
    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Usado hoje só pelo endpoint de desenvolvimento que gera um token de teste.</summary>
    Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken);
}
