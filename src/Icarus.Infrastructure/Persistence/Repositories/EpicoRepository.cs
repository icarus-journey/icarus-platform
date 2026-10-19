using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class EpicoRepository(IcarusDbContext context) : IEpicoRepository
{
    public async Task AdicionarAsync(Epico epico, CancellationToken cancellationToken)
        => await context.Epicos.AddAsync(epico, cancellationToken);

    public Task<Epico?> BuscarComCampanhaEMissoesAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => context.Epicos
            .Include(e => e.Campanhas)
            .ThenInclude(c => c.Missoes)
            .SingleOrDefaultAsync(e => e.Id == epicoId && e.UsuarioId == usuarioId, cancellationToken);
}
