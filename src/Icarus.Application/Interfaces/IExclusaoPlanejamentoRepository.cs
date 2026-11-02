using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>Carrega o planejamento completo para exclusão atômica, sem filtros operacionais.</summary>
public interface IExclusaoPlanejamentoRepository
{
    Task<Epico?> BuscarEpicoAsync(long id, Guid usuarioId, CancellationToken ct);
    Task<Campanha?> BuscarCampanhaAsync(long id, Guid usuarioId, CancellationToken ct);
    Task<Missao?> BuscarMissaoAsync(long id, Guid usuarioId, CancellationToken ct);
    void RemoverRascunhos(Campanha campanha);
}
