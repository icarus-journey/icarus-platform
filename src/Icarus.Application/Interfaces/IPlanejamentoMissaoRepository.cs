using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

public interface IPlanejamentoMissaoRepository
{
    Task<Missao?> BuscarAsync(long missaoId, Guid usuarioId, CancellationToken ct);
}
