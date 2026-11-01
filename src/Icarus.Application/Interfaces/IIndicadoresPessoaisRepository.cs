using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;

namespace Icarus.Application.Interfaces;

public interface IIndicadoresPessoaisRepository
{
    Task<DadosRelatorioSemanal> CalcularRelatorioAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct);
    Task<IndicadoresPessoais> CalcularAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct);
    Task<ProgressoCalculado> CalcularProgressoAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct);
}
