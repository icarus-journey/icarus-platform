using System.Data;
using Icarus.Application.Common.Analise;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarExtratoPontos;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class ExtratoPontosRepository(IcarusDbContext contexto) : IExtratoPontosRepository
{
    private static IQueryable<MovimentacaoPontos> ComOrigem(IQueryable<MovimentacaoPontos> q)
        => q.Include(m => m.Item).Include(m => m.Ocorrencia).ThenInclude(o => o!.Missao).ThenInclude(m => m.Recorrencia).AsSingleQuery();

    public async Task<LoteExtratoPontos> ListarAsync(Guid titular, FiltroExtratoPontos filtro, DateTimeOffset referencia,
        string fuso, CursorExtratoPontos? cursor, CancellationToken ct)
    {
        await using var leitura = contexto.Database.CurrentTransaction is null
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var saldo = await contexto.Usuarios.AsNoTracking().Where(u => u.Id == titular).Select(u => (long?)u.SaldoPontos).SingleOrDefaultAsync(ct)
            ?? throw new PerfilNaoEncontradoException();
        var q = contexto.MovimentacoesPontos.AsNoTracking().Where(m => m.UsuarioId == titular && m.CriadoEm <= referencia);
        var limiteId = cursor?.LimiteId ?? await q.MaxAsync(m => (long?)m.Id, ct) ?? 0;
        q = q.Where(m => m.Id <= limiteId);
        if (filtro.Inicio is { } inicio && filtro.Fim is { } fim)
        {
            var periodo = new PeriodoAnalitico(inicio, fim, fuso);
            var de = periodo.InicioUtc; var ate = periodo.FimExclusivoUtc;
            q = q.Where(m => m.CriadoEm >= de && m.CriadoEm < ate);
        }
        if (filtro.Natureza == NaturezaExtrato.Recebidos) q = q.Where(m => m.Quantidade > 0);
        if (filtro.Natureza == NaturezaExtrato.Usados) q = q.Where(m => m.Quantidade < 0);
        if (cursor is not null) q = q.Where(m => m.CriadoEm < cursor.UltimaData || (m.CriadoEm == cursor.UltimaData && m.Id < cursor.UltimoId));
        var linhas = await ComOrigem(q.OrderByDescending(m => m.CriadoEm).ThenByDescending(m => m.Id).Take(filtro.Limite + 1)).ToListAsync(ct);
        var lote = new LoteExtratoPontos(saldo, linhas.Take(filtro.Limite).Select(m => MovimentoExtratoPontos.DoMovimento(m, fuso)).ToArray(),
            linhas.Count > filtro.Limite, limiteId);
        if (leitura is not null) await leitura.CommitAsync(ct);
        return lote;
    }
    public async Task<MovimentoExtratoPontos?> ConsultarAsync(Guid titular, long id, string fuso, CancellationToken ct)
    {
        var m = await ComOrigem(contexto.MovimentacoesPontos.AsNoTracking().Where(m => m.UsuarioId == titular && m.Id == id)).SingleOrDefaultAsync(ct);
        return m is null ? null : MovimentoExtratoPontos.DoMovimento(m, fuso);
    }
}
