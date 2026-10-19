using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="Campanha"/>. A implementação concreta (EF Core) fica em
/// Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface ICampanhaRepository
{
    Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken);

    /// <summary>
    /// Carrega a campanha restrita ao usuário dono (RF-02.4) — a posse é verificada pelo
    /// usuário do épico ao qual ela pertence, já que campanha não tem UsuarioId próprio.
    /// </summary>
    Task<Campanha?> BuscarPorIdAsync(long campanhaId, Guid usuarioId, CancellationToken cancellationToken);
}
