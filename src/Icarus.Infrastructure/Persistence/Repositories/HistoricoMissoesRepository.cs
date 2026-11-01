using System.Data;
using Icarus.Application.Common;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarHistoricoMissoes;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class HistoricoMissoesRepository(IcarusDbContext contexto) : IHistoricoMissoesRepository
{
    private static IQueryable<Ocorrencia> ComDetalhes(IQueryable<Ocorrencia> q) => q
        .Include(o => o.Missao).ThenInclude(m => m.Recorrencia)
        .Include(o => o.Missao).ThenInclude(m => m.Campanha).ThenInclude(c => c!.Epico)
        .Include(o => o.MovimentacoesPontos).AsSingleQuery();

    public async Task<LoteHistoricoMissoes> ListarAsync(Guid titular, FiltroHistoricoMissoes filtro, DateTimeOffset referencia,
        string fuso, CursorHistoricoMissoes? cursor, CancellationToken ct)
    {
        await using var leitura = contexto.Database.CurrentTransaction is null
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var q = contexto.Ocorrencias.AsNoTracking().Where(o => o.Missao.UsuarioId == titular && o.CriadoEm <= referencia);
        var limiteId = cursor?.LimiteId ?? await q.MaxAsync(o => (long?)o.Id, ct) ?? 0;
        q = q.Where(o => o.Id <= limiteId && o.DataPrevista >= filtro.Inicio && o.DataPrevista <= filtro.Fim);
        if (filtro.CampanhaId is { } campanha) q = q.Where(o => o.Missao.CampanhaId == campanha);
        if (filtro.Avulsas) q = q.Where(o => o.Missao.CampanhaId == null);
        var hoje = FusoHorario.DataLocal(referencia, fuso);
        q = filtro.Estado switch
        {
            EstadoHistoricoMissao.Pendente => q.Where(o => (o.Status == StatusOcorrencia.Pendente || o.Status == StatusOcorrencia.Atrasada) && o.DataPrevista >= hoje),
            EstadoHistoricoMissao.Atrasada => q.Where(o => (o.Status == StatusOcorrencia.Pendente || o.Status == StatusOcorrencia.Atrasada) && o.DataPrevista < hoje),
            EstadoHistoricoMissao.Feita => q.Where(o => o.Status == StatusOcorrencia.Concluida),
            EstadoHistoricoMissao.FeitaComAtraso => q.Where(o => o.Status == StatusOcorrencia.ConcluidaComAtraso),
            EstadoHistoricoMissao.Cancelada => q.Where(o => o.Status == StatusOcorrencia.Excluida),
            _ => q
        };
        if (cursor is not null) q = q.Where(o => o.DataPrevista < cursor.UltimaData || (o.DataPrevista == cursor.UltimaData && o.Id < cursor.UltimoId));
        var linhas = await ComDetalhes(q.OrderByDescending(o => o.DataPrevista).ThenByDescending(o => o.Id).Take(filtro.Limite + 1)).ToListAsync(ct);
        var parcial = await contexto.Missoes.AsNoTracking().AnyAsync(m => m.UsuarioId == titular && m.ConcluidaEm != null
            && !m.Ocorrencias.Any(o => o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso), ct);
        var resultado = new LoteHistoricoMissoes(linhas.Take(filtro.Limite).Select(o => RegistroHistoricoMissao.DaOcorrencia(o, hoje)).ToArray(),
            linhas.Count > filtro.Limite, limiteId, parcial);
        if (leitura is not null) await leitura.CommitAsync(ct);
        return resultado;
    }
    public async Task<RegistroHistoricoMissao?> ConsultarAsync(Guid titular, long ocorrenciaId, DateOnly hoje, CancellationToken ct)
    {
        var o = await ComDetalhes(contexto.Ocorrencias.AsNoTracking().Where(o => o.Id == ocorrenciaId && o.Missao.UsuarioId == titular)).SingleOrDefaultAsync(ct);
        return o is null ? null : RegistroHistoricoMissao.DaOcorrencia(o, hoje);
    }
    public async Task<IReadOnlyList<CampanhaHistoricoMissao>> ListarCampanhasAsync(Guid titular, CancellationToken ct)
        => await contexto.Campanhas.AsNoTracking().Where(c => c.Epico.UsuarioId == titular
                && c.Missoes.Any(m => m.UsuarioId == titular && m.Ocorrencias.Count > 0))
            .OrderBy(c => c.Titulo).ThenBy(c => c.Id).Select(c => new CampanhaHistoricoMissao(c.Id, c.Titulo,
                c.Estado == EstadoCampanha.Excluida || c.Epico.Estado == EstadoEpico.Excluido)).ToListAsync(ct);
}
