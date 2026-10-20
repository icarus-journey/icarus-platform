using Icarus.Application.Common.Analise;

namespace Icarus.Application.Interfaces;

public interface IIndicadoresPessoaisRepository
{
    Task<IndicadoresPessoais> CalcularAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct);
}
