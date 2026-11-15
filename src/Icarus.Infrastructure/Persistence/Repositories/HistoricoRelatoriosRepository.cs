using System.Data;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarHistoricoRelatorios;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class HistoricoRelatoriosRepository(IcarusDbContext contexto) : IHistoricoRelatoriosRepository
{
    private sealed class Registro { public Relatorio Relatorio { get; set; } = null!; public string? Fuso { get; set; } }
    private IQueryable<Registro> ComFuso(IQueryable<Relatorio> relatorios) =>
        from r in relatorios
        join t in contexto.Set<TrabalhoRelatorioSemanal>().AsNoTracking()
            on new { r.UsuarioId, r.PeriodoInicio, r.PeriodoFim } equals new { t.UsuarioId, t.PeriodoInicio, t.PeriodoFim } into trabalhos
        from t in trabalhos.DefaultIfEmpty()
        select new Registro { Relatorio = r, Fuso = t == null ? null : t.FusoHorario };
    private IQueryable<Relatorio> DoTitular(Guid titular) => contexto.Relatorios.AsNoTracking().Where(r => r.UsuarioId == titular && r.Tipo == TipoRelatorio.Semanal);
    public async Task<LoteHistoricoRelatorios> ListarAsync(Guid titular, int limite, DateTimeOffset referencia, CursorHistoricoRelatorios? cursor, CancellationToken ct)
    {
        await using var leitura = contexto.Database.CurrentTransaction is null
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var q = DoTitular(titular).Where(r => r.CriadoEm <= referencia);
        var ancora = cursor?.LimiteId ?? await q.MaxAsync(r => (long?)r.Id, ct) ?? 0;
        q = q.Where(r => r.Id <= ancora);
        if (cursor is not null) q = q.Where(r => r.PeriodoInicio < cursor.UltimoInicio || (r.PeriodoInicio == cursor.UltimoInicio && r.Id < cursor.UltimoId));
        var linhas = await ComFuso(q).OrderByDescending(r => r.Relatorio.PeriodoInicio).ThenByDescending(r => r.Relatorio.Id).Take(limite + 1).ToListAsync(ct);
        var resultado = new LoteHistoricoRelatorios(linhas.Take(limite).Select(r => ApresentacaoHistoricoRelatorios.Resumir(r.Relatorio, r.Fuso)).ToArray(), linhas.Count > limite, ancora);
        if (leitura is not null) await leitura.CommitAsync(ct);
        return resultado;
    }
    public async Task<DetalheRelatorioSalvo?> ConsultarAsync(Guid titular, long id, CancellationToken ct)
    {
        var r = await ComFuso(DoTitular(titular).Where(r => r.Id == id)).SingleOrDefaultAsync(ct);
        return r is null ? null : ApresentacaoHistoricoRelatorios.Detalhar(r.Relatorio, r.Fuso) ?? throw new RelatorioSalvoIndisponivelException();
    }
}
