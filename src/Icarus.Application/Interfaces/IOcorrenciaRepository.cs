using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="Ocorrencia"/>. A implementação concreta (EF Core) fica em
/// Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface IOcorrenciaRepository
{
    Task AdicionarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken);

    /// <summary>
    /// Busca a ocorrência de uma missão numa data prevista (missao_id + data_prevista é único —
    /// DER seção 12). Para missão não recorrente existe no máximo uma linha.
    /// </summary>
    Task<Ocorrencia?> BuscarPorMissaoEDataAsync(long missaoId, DateOnly dataPrevista, CancellationToken cancellationToken);
}
