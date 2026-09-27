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

    Task AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken);
}
