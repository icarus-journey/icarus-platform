using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class TokenRenovacaoRepository(IcarusDbContext context) : ITokenRenovacaoRepository
{
    public async Task AdicionarAsync(TokenRenovacao tokenRenovacao, CancellationToken cancellationToken)
        => await context.TokensRenovacao.AddAsync(tokenRenovacao, cancellationToken);

    public Task<TokenRenovacao?> BuscarPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => context.TokensRenovacao.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task RevogarTodosDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var tokensAtivos = await context.TokensRenovacao
            .Where(t => t.UsuarioId == usuarioId && t.RevogadoEm == null)
            .ToListAsync(cancellationToken);

        var agora = DateTimeOffset.UtcNow;
        foreach (var token in tokensAtivos)
        {
            token.RevogadoEm = agora;
        }
    }
}
