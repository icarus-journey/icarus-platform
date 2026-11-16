using Icarus.Application.Common.Analise;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

internal static class LeitorPanoramaObjetivos
{
    internal static async Task<PanoramaObjetivos> LerAsync(IcarusDbContext contexto, Guid usuarioId,
        PeriodoAnalitico periodo, DateTimeOffset agora, CancellationToken ct)
    {
        var epicos = await contexto.Epicos.AsNoTracking().Where(e => e.UsuarioId == usuarioId && e.Estado != EstadoEpico.Excluido)
            .Include(e => e.Campanhas).AsSingleQuery().ToListAsync(ct);
        var registros = await contexto.Ocorrencias.AsNoTracking().Where(o => o.Missao.UsuarioId == usuarioId && o.Missao.CampanhaId != null
            && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(o => new AtividadeObjetivo(o.Missao.CampanhaId!.Value, o.Missao.Campanha!.EpicoId, o.RealizadaEm, o.ConcluidaEm)).ToListAsync(ct);
        registros.AddRange(await contexto.Missoes.AsNoTracking().Where(m => m.UsuarioId == usuarioId && m.CampanhaId != null
            && m.ConcluidaEm != null && !m.Ocorrencias.Any(o => o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(m => new AtividadeObjetivo(m.CampanhaId!.Value, m.Campanha!.EpicoId, null, m.ConcluidaEm)).ToListAsync(ct));
        var medicoes = await contexto.MedicoesCampanhas.AsNoTracking().Where(m => m.Campanha.Epico.UsuarioId == usuarioId).ToListAsync(ct);
        return CalculadorPanoramaObjetivos.Calcular(epicos, registros, medicoes, periodo, agora);
    }
}
