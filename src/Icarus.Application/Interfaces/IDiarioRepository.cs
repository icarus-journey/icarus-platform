using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

public interface IDiarioRepository
{
    Task AdicionarAsync(Diario entrada, CancellationToken cancellationToken);
    Task<List<Diario>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Diario?> BuscarPorIdAsync(long entradaId, Guid usuarioId, CancellationToken cancellationToken);
}
