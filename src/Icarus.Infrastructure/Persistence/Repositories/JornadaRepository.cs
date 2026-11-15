using System.Data;
using Icarus.Application.Common.Jornada;
using Icarus.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class JornadaRepository(IcarusDbContext contexto) : IJornadaRepository
{
    public async Task<DadosJornada?> BuscarAsync(Guid usuarioId, CancellationToken ct)
    {
        // Saldo e conclusões pertencem ao mesmo snapshot, inclusive durante uma conclusão concorrente.
        await using var leitura = await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var usuario = await contexto.Usuarios.AsNoTracking().Where(u => u.Id == usuarioId)
            .Select(u => new { u.SaldoPontos, u.FusoHorario }).SingleOrDefaultAsync(ct);
        if (usuario is null) return null;
        var realizacoes = await LeitorRealizacoes.LerAsync(contexto, usuarioId, ct);
        var protegidos = await contexto.Set<Icarus.Domain.Entities.FechamentoJornada>().AsNoTracking()
            .Where(d => d.UsuarioId == usuarioId && d.Estado == "protegido").Select(d => d.Data).ToListAsync(ct);
        var pendentes = await contexto.Set<Icarus.Domain.Entities.ConsumoProtecao>().AsNoTracking()
            .CountAsync(x => x.UsuarioId == usuarioId && x.CompensadoEm != null && x.DevolucaoId == null, ct);
        await leitura.CommitAsync(ct);
        return new(usuario.SaldoPontos, usuario.FusoHorario, realizacoes, protegidos, pendentes);
    }
}
