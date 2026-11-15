using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class PlanejamentoMissaoRepository(IcarusDbContext contexto) : IPlanejamentoMissaoRepository
{
    public Task<Missao?> BuscarAsync(long missaoId, Guid usuarioId, CancellationToken ct)
        => contexto.Missoes.Include(m => m.Usuario).Include(m => m.Recorrencia)
            .Include(m => m.Campanha).ThenInclude(c => c!.Epico)
            .Include(m => m.Ocorrencias).ThenInclude(o => o.MovimentacoesPontos).AsSingleQuery()
            .SingleOrDefaultAsync(m => m.Id == missaoId && m.UsuarioId == usuarioId && m.Status != StatusMissao.Excluida
                && (m.Campanha == null || (m.Campanha.Estado != EstadoCampanha.Excluida && m.Campanha.Epico.Estado != EstadoEpico.Excluido)), ct);
}
