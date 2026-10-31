using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class ExclusaoPlanejamentoRepository(IcarusDbContext contexto) : IExclusaoPlanejamentoRepository
{
    // Consulta única: o grafo e seus tokens devem representar a mesma leitura do PostgreSQL.
    public Task<Epico?> BuscarEpicoAsync(long id, Guid usuarioId, CancellationToken ct)
        => contexto.Epicos.Include(e => e.Usuario)
            .Include(e => e.Campanhas).ThenInclude(c => c.Missoes).ThenInclude(m => m.Recorrencia)
            .Include(e => e.Campanhas).ThenInclude(c => c.Missoes).ThenInclude(m => m.Ocorrencias)
            .Include(e => e.Campanhas).ThenInclude(c => c.MissoesIA)
            .AsSingleQuery().SingleOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId, ct);

    public Task<Campanha?> BuscarCampanhaAsync(long id, Guid usuarioId, CancellationToken ct)
        => contexto.Campanhas.Include(c => c.Epico).ThenInclude(e => e.Usuario)
            .Include(c => c.Missoes).ThenInclude(m => m.Recorrencia)
            .Include(c => c.Missoes).ThenInclude(m => m.Ocorrencias)
            .Include(c => c.MissoesIA)
            .AsSingleQuery().SingleOrDefaultAsync(c => c.Id == id && c.Epico.UsuarioId == usuarioId, ct);

    public Task<Missao?> BuscarMissaoAsync(long id, Guid usuarioId, CancellationToken ct)
        => contexto.Missoes.Include(m => m.Usuario).Include(m => m.Recorrencia).Include(m => m.Ocorrencias)
            .AsSingleQuery().SingleOrDefaultAsync(m => m.Id == id && m.UsuarioId == usuarioId, ct);

    public void RemoverRascunhos(Campanha campanha)
        => contexto.MissoesIA.RemoveRange(campanha.MissoesIA
            .Where(m => m.Status == StatusMissao.Rascunho && m.MissaoId == null));
}
