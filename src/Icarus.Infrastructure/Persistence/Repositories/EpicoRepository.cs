using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class EpicoRepository(IcarusDbContext context) : IEpicoRepository
{
    public async Task AdicionarAsync(Epico epico, CancellationToken cancellationToken)
        => await context.Epicos.AddAsync(epico, cancellationToken);

    public Task<Epico?> BuscarComCampanhaEMissoesAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => context.Epicos
            .Include(e => e.Campanhas.Where(c => c.Estado != EstadoCampanha.Excluida))
            .ThenInclude(c => c.Missoes.Where(m => m.Status != StatusMissao.Excluida))
            .ThenInclude(m => m.Recorrencia)
            .SingleOrDefaultAsync(e => e.Id == epicoId && e.UsuarioId == usuarioId && e.Estado != EstadoEpico.Excluido, cancellationToken);

    public Task<Epico?> BuscarPorIdAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => context.Epicos.Include(e => e.Usuario).SingleOrDefaultAsync(e => e.Id == epicoId && e.UsuarioId == usuarioId && e.Estado != EstadoEpico.Excluido, cancellationToken);

    public async Task<List<Epico>> ListarComCampanhasEMissoesAsync(Guid usuarioId, CancellationToken cancellationToken)
        => await context.Epicos.AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId && e.Estado != EstadoEpico.Excluido)
            .Include(e => e.Campanhas.Where(c => c.Estado != EstadoCampanha.Excluida))
            .ThenInclude(c => c.Missoes.Where(m => m.Status != StatusMissao.Excluida))
            .ThenInclude(m => m.Recorrencia)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);
}
