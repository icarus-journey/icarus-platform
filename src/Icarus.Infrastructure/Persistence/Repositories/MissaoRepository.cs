using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class MissaoRepository(IcarusDbContext context) : IMissaoRepository
{
    public async Task AdicionarAsync(Missao missao, CancellationToken cancellationToken)
        => await context.Missoes.AddAsync(missao, cancellationToken);

    public Task<Missao?> BuscarPorIdAsync(long missaoId, Guid usuarioId, CancellationToken cancellationToken)
        => context.Missoes
            .Include(m => m.Recorrencia)
            .SingleOrDefaultAsync(m => m.Id == missaoId && m.UsuarioId == usuarioId && m.Status != StatusMissao.Excluida
                && (m.CampanhaId == null || (m.Campanha!.Estado != EstadoCampanha.Excluida && m.Campanha.Epico.Estado != EstadoEpico.Excluido)), cancellationToken);

    public async Task<List<Missao>> ListarAvulsasAsync(Guid usuarioId, CancellationToken cancellationToken)
        => await context.Missoes.AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId && m.CampanhaId == null && m.Status != StatusMissao.Excluida)
            .Include(m => m.Recorrencia)
            .OrderByDescending(m => m.Id)
            .ToListAsync(cancellationToken);

    public async Task<List<Missao>> ListarRecorrentesAtivasAsync(CancellationToken cancellationToken)
        => await context.Missoes
            .Where(m => m.Status == StatusMissao.Ativa && m.Recorrencia != null)
            .Include(m => m.Recorrencia)
            .Include(m => m.Campanha).ThenInclude(c => c!.Epico).ThenInclude(e => e.Usuario)
            .ToListAsync(cancellationToken);
}
