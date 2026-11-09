using System.Data;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence;

public sealed class TransacaoConclusaoMissao(IcarusDbContext contexto) : ITransacaoConclusaoMissao
{
    public async Task<T> ExecutarAsync<T>(Guid usuarioId, Func<CancellationToken, Task<T>> operacao, CancellationToken ct)
    {
        if (contexto.Database.CurrentTransaction is not null || contexto.ChangeTracker.HasChanges())
            throw new InvalidOperationException("A conclusão exige contexto sem transação ou alterações pendentes.");

        // Uma consulta anterior no escopo não pode fornecer saldo/execução desatualizados após a espera.
        contexto.ChangeTracker.Clear();
        await using var transacao = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // Mesma ordem da loja: saldo antes dos demais registros. A espera ocorre no PostgreSQL.
            _ = await contexto.Usuarios.FromSqlInterpolated(
                $"SELECT * FROM usuario WHERE id = {usuarioId} FOR UPDATE").SingleOrDefaultAsync(ct)
                ?? throw new PerfilNaoEncontradoException();
            var resultado = await operacao(ct);
            await transacao.CommitAsync(ct);
            return resultado;
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
}
