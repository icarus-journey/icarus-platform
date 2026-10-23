using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="Recorrencia"/>. A implementação concreta (EF Core) fica em
/// Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface IRecorrenciaRepository
{
    Task AdicionarAsync(Recorrencia recorrencia, CancellationToken cancellationToken);
}
