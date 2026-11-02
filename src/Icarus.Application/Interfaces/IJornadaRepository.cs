using Icarus.Application.Common.Jornada;

namespace Icarus.Application.Interfaces;

public interface IJornadaRepository
{
    Task<DadosJornada?> BuscarAsync(Guid usuarioId, CancellationToken ct);
}
