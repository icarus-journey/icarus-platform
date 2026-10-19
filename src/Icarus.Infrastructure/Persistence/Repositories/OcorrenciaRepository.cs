using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class OcorrenciaRepository(IcarusDbContext context) : IOcorrenciaRepository
{
    public async Task AdicionarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken)
        => await context.Ocorrencias.AddAsync(ocorrencia, cancellationToken);

    public Task<Ocorrencia?> BuscarPorMissaoEDataAsync(long missaoId, DateOnly dataPrevista, CancellationToken cancellationToken)
        => context.Ocorrencias.SingleOrDefaultAsync(o => o.MissaoId == missaoId && o.DataPrevista == dataPrevista, cancellationToken);
}
