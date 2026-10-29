using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class RecorrenciaRepository(IcarusDbContext context) : IRecorrenciaRepository
{
    public async Task AdicionarAsync(Recorrencia recorrencia, CancellationToken cancellationToken)
        => await context.Recorrencias.AddAsync(recorrencia, cancellationToken);
}
