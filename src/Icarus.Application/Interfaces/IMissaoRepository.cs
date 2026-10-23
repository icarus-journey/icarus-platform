using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="Missao"/>. A implementação concreta (EF Core) fica em
/// Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface IMissaoRepository
{
    Task AdicionarAsync(Missao missao, CancellationToken cancellationToken);

    /// <summary>Carrega a missão (com a recorrência, se houver) restrita ao usuário dono (RF-02.4).</summary>
    Task<Missao?> BuscarPorIdAsync(long missaoId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Missões ativas com recorrência configurada — usado pela manutenção diária da janela (RF-04.5).</summary>
    Task<List<Missao>> ListarRecorrentesAtivasAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lista as missões avulsas do usuário (CampanhaId nulo, RF-03). Missões atreladas a uma
    /// campanha já são listadas por <see cref="IEpicoRepository.ListarComCampanhasEMissoesAsync"/>.
    /// </summary>
    Task<List<Missao>> ListarAvulsasAsync(Guid usuarioId, CancellationToken cancellationToken);
}
