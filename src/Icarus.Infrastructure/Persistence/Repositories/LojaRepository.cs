using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class LojaRepository(IcarusDbContext contexto) : ILojaRepository
{
    public async Task ExecutarAsync(Guid usuarioId, Func<Usuario, CancellationToken, Task> operacao, CancellationToken cancelamento)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancelamento);
        try
        {
            // Ordem única: primeiro saldo, depois itens/resgates. Serializa também o limite do inventário.
            var usuario = await contexto.Usuarios.FromSqlInterpolated(
                $"SELECT * FROM usuario WHERE id = {usuarioId} FOR UPDATE").SingleOrDefaultAsync(cancelamento)
                ?? throw new PerfilNaoEncontradoException();
            await operacao(usuario, cancelamento);
            await transacao.CommitAsync(cancelamento);
        }
        catch
        {
            await transacao.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            contexto.ChangeTracker.Clear();
        }
    }

    public Task<List<Item>> ListarItensAsync(Guid usuarioId, CancellationToken cancelamento)
        => contexto.Itens.Where(i => i.UsuarioId == usuarioId).ToListAsync(cancelamento);

    public Task<List<ResgateItem>> ListarResgatesAsync(Guid usuarioId, CancellationToken cancelamento)
        => contexto.Set<ResgateItem>().Include(r => r.Item).Where(r => r.UsuarioId == usuarioId).ToListAsync(cancelamento);

    public async Task AdicionarItemAsync(Item item, CancellationToken cancelamento)
        => await contexto.Itens.AddAsync(item, cancelamento);
    public async Task AdicionarResgateAsync(ResgateItem resgate, CancellationToken cancelamento)
        => await contexto.Set<ResgateItem>().AddAsync(resgate, cancelamento);
    public async Task AdicionarMovimentacaoAsync(MovimentacaoPontos movimentacao, CancellationToken cancelamento)
        => await contexto.MovimentacoesPontos.AddAsync(movimentacao, cancelamento);
}
