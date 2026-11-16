using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

public interface IMedicaoCampanhaRepository
{
    Task AdicionarAsync(MedicaoCampanha medicao, CancellationToken ct);
    Task<MedicaoCampanha?> BuscarOperacaoAsync(long campanhaId, Guid usuarioId, Guid operacaoId, CancellationToken ct);
    Task<IReadOnlyList<MedicaoCampanha>> ListarAsync(long campanhaId, Guid usuarioId, CancellationToken ct);
}
