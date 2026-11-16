using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Messaging;

public sealed class ProcessadorJornada(IcarusDbContext contexto, IProtecaoSequencia protecoes, TimeProvider relogio)
{
    public async Task ExecutarAsync(ComandoJornada comando, CancellationToken ct)
    {
        if (comando.TrabalhoId is { } trabalhoId)
        {
            var usuarioId = await contexto.Set<OutboxJornada>().AsNoTracking()
                .Where(x => x.Id == trabalhoId && x.ConcluidaEm == null).Select(x => (Guid?)x.UsuarioId).SingleOrDefaultAsync(ct);
            if (usuarioId is { } id) await FecharUsuarioAsync(id, trabalhoId, ct);
            return;
        }
        // O trabalhador solicita o ciclo; só a API enumera titulares e acessa os dados de negócio.
        var pagina = 0;
        while (true)
        {
            var ids = await contexto.Usuarios.AsNoTracking().OrderBy(x => x.Id).Skip(pagina * 100).Take(100)
                .Select(x => x.Id).ToListAsync(ct);
            foreach (var id in ids) await FecharUsuarioAsync(id, null, ct);
            if (ids.Count < 100) break;
            pagina++;
        }
    }

    private async Task FecharUsuarioAsync(Guid id, Guid? trabalhoId, CancellationToken ct)
    {
        contexto.ChangeTracker.Clear();
        await using var transacao = await contexto.Database.BeginTransactionAsync(ct);
        try
        {
            var usuario = await contexto.Usuarios.FromSqlInterpolated($"SELECT * FROM usuario WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (usuario is null) return;
            var trabalho = trabalhoId is { } chave ? await contexto.Set<OutboxJornada>().SingleOrDefaultAsync(x => x.Id == chave, ct) : null;
            if (trabalho?.ConcluidaEm is not null) return;
            await protecoes.ReconciliarAsync(usuario, ct);
            if (trabalho is not null) trabalho.ConcluidaEm = relogio.GetUtcNow();
            await contexto.SaveChangesAsync(ct);
            await transacao.CommitAsync(ct);
        }
        finally { contexto.ChangeTracker.Clear(); }
    }
}
