using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class MovimentacaoPontosRepository(IcarusDbContext context) : IMovimentacaoPontosRepository
{
    public async Task AdicionarAsync(MovimentacaoPontos movimentacao, CancellationToken cancellationToken)
        => await context.MovimentacoesPontos.AddAsync(movimentacao, cancellationToken);
}
