using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="TrabalhoOnboarding"/>. A implementação concreta (EF Core)
/// fica em Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface ITrabalhoOnboardingRepository
{
    Task AdicionarAsync(TrabalhoOnboarding trabalho, CancellationToken cancellationToken);

    /// <summary>Restrito ao usuário dono (RF-02.4) — nulo tanto se não existe quanto se é de outro usuário.</summary>
    Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Usado pelo consumer do resultado assíncrono: não há usuário autenticado na mensagem
    /// da fila, então a busca é só pelo job_id (idempotência / correlação).
    /// </summary>
    Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken);
}
