using Icarus.Domain.Entities;

namespace Icarus.Application.Interfaces;

/// <summary>
/// Acesso a dados de <see cref="MovimentacaoPontos"/>. A implementação concreta (EF Core) fica
/// em Icarus.Infrastructure — o resto do código depende só deste contrato.
/// </summary>
public interface IMovimentacaoPontosRepository
{
    Task AdicionarAsync(MovimentacaoPontos movimentacao, CancellationToken cancellationToken);
}
