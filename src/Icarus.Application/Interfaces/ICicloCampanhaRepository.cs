using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

public interface ICicloCampanhaRepository
{
    Task<List<string?>> ListarFusosPlanejadosAsync(Guid? usuarioId, CancellationToken ct);

    /// <summary>Transição atômica e condicional; nunca reabre campanhas encerradas.</summary>
    Task<int> IniciarPlanejadasAsync(Guid? usuarioId, string? fusoHorario, DateOnly hoje, DateTimeOffset agora, CancellationToken ct);

    /// <summary>Carrega somente o planejamento cancelável das recorrentes do titular.</summary>
    Task<List<Missao>> ListarRecorrentesParaEncerrarAsync(long campanhaId, Guid usuarioId, DateOnly hoje, CancellationToken ct);
}
