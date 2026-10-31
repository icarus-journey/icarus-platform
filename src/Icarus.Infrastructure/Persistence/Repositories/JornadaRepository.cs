using System.Data;
using Icarus.Application.Common.Jornada;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
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
        var realizacoes = await contexto.Ocorrencias.AsNoTracking()
            .Where(o => o.Missao.UsuarioId == usuarioId
                && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(o => new RealizacaoSequencia(o.RealizadaEm, o.FusoHorarioRealizacao, o.ConcluidaEm)).ToListAsync(ct);
        var legadas = await contexto.Missoes.AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId && m.ConcluidaEm != null
                && !m.Ocorrencias.Any(o => o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(m => new RealizacaoSequencia(null, null, m.ConcluidaEm)).ToListAsync(ct);
        realizacoes.AddRange(legadas);
        await leitura.CommitAsync(ct);
        return new(usuario.SaldoPontos, usuario.FusoHorario, realizacoes);
    }
}
