using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class MedicaoCampanhaRepository(IcarusDbContext contexto) : IMedicaoCampanhaRepository
{
    public async Task AdicionarAsync(MedicaoCampanha medicao, CancellationToken ct)
        => await contexto.Set<MedicaoCampanha>().AddAsync(medicao, ct);

    public Task<MedicaoCampanha?> BuscarOperacaoAsync(long campanhaId, Guid usuarioId, Guid operacaoId, CancellationToken ct)
        => Consulta(campanhaId, usuarioId).SingleOrDefaultAsync(m => m.OperacaoId == operacaoId, ct);

    public async Task<IReadOnlyList<MedicaoCampanha>> ListarAsync(long campanhaId, Guid usuarioId, CancellationToken ct)
        => await Consulta(campanhaId, usuarioId).OrderByDescending(m => m.RegistradaEm).ThenByDescending(m => m.Id).ToListAsync(ct);

    private IQueryable<MedicaoCampanha> Consulta(long campanhaId, Guid usuarioId)
        => contexto.Set<MedicaoCampanha>().AsNoTracking().Where(m => m.CampanhaId == campanhaId && m.Campanha.Epico.UsuarioId == usuarioId);
}
