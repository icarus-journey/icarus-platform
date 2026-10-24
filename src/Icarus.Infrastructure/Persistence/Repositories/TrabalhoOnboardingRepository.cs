using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class TrabalhoOnboardingRepository(IcarusDbContext context) : ITrabalhoOnboardingRepository
{
    public async Task AdicionarAsync(TrabalhoOnboarding trabalho, CancellationToken cancellationToken)
        => await context.TrabalhosOnboarding.AddAsync(trabalho, cancellationToken);

    public Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken)
        => context.TrabalhosOnboarding.SingleOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId, cancellationToken);

    public Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
        => context.TrabalhosOnboarding.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
}
