using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class CampanhaRepository(IcarusDbContext context) : ICampanhaRepository
{
    public async Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
        => await context.Campanhas.AddAsync(campanha, cancellationToken);

    public Task<Campanha?> BuscarPorIdAsync(long campanhaId, Guid usuarioId, CancellationToken cancellationToken)
        => context.Campanhas
            .Include(c => c.Epico).ThenInclude(e => e.Usuario)
            .SingleOrDefaultAsync(c => c.Id == campanhaId && c.Epico.UsuarioId == usuarioId, cancellationToken);
}
