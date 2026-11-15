using Icarus.Application.Common.Analise;

namespace Icarus.Application.Interfaces;

public interface IReferenciasPublicasRepository
{
    Task<SnapshotReferenciasPublicas?> ObterVigenteAsync(string areaDaVida, CancellationToken ct);
}
