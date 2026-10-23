using Icarus.Application.Common.Analise;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class IndicadoresPessoaisRepository(IcarusDbContext contexto) : IIndicadoresPessoaisRepository
{
    public async Task<IndicadoresPessoais> CalcularAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct)
        => (await CalcularProgressoAsync(usuarioId, periodo, ct)).Indicadores;

    public async Task<ProgressoCalculado> CalcularProgressoAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct)
    {
        var inicio = periodo.InicioUtc; var fim = periodo.FimExclusivoUtc;
        var conclusoes = await contexto.Ocorrencias.AsNoTracking()
            .Where(o => o.Missao.UsuarioId == usuarioId && o.RealizadaEm >= inicio && o.RealizadaEm < fim
                && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(o => new { o.RealizadaEm, o.Status, o.Missao.CampanhaId,
                Area = o.Missao.Campanha != null ? (AreaDaVida?)o.Missao.Campanha.AreaDaVida : null })
            .ToListAsync(ct);
        var movimentos = await contexto.MovimentacoesPontos.AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId && m.CriadoEm >= inicio && m.CriadoEm < fim)
            .Select(m => new { m.Tipo, m.Quantidade }).ToListAsync(ct);
        var resgates = await contexto.Set<Icarus.Domain.Entities.ResgateItem>().AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId && ((r.ResgatadoEm >= inicio && r.ResgatadoEm < fim)
                || (r.UtilizadoEm >= inicio && r.UtilizadoEm < fim)))
            .Select(r => new { r.ResgatadoEm, r.UtilizadoEm }).ToListAsync(ct);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(periodo.FusoHorario);
        var areas = conclusoes.Where(o => o.Area is not null).GroupBy(o => o.Area!.Value)
            .Select(g => new ConclusoesPorArea(CatalogoAreasAnaliticas.Areas.Single(a => a.Area == g.Key).Codigo, g.Count()))
            .OrderBy(a => a.AreaDaVida, StringComparer.Ordinal).ToArray();
        var ganhos = movimentos.Where(m => m.Quantidade > 0 && (m.Tipo is TipoMovimentacaoPontos.GanhoMissao or TipoMovimentacaoPontos.Bonus)).ToArray();
        var gastos = movimentos.Where(m => m.Quantidade < 0 && m.Tipo == TipoMovimentacaoPontos.CompraItem).ToArray();
        var compras = resgates.Count(r => r.ResgatadoEm >= inicio && r.ResgatadoEm < fim);
        var usos = resgates.Count(r => r.UtilizadoEm >= inicio && r.UtilizadoEm < fim);
        var maiorAtividade = conclusoes.Where(o => o.CampanhaId is not null).GroupBy(o => o.CampanhaId)
            .Select(g => (int?)g.Count()).OrderDescending().FirstOrDefault();
        var datas = conclusoes.Select(o => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(o.RealizadaEm!.Value, fuso).DateTime)).ToArray();
        var indicadores = new IndicadoresPessoais(null, conclusoes.Count == 0 ? null : conclusoes.Count, null, null, null,
            conclusoes.Count == 0 ? null : conclusoes.Count(o => o.Status == StatusOcorrencia.ConcluidaComAtraso), null,
            ganhos.Length == 0 ? null : ganhos.Sum(m => m.Quantidade), gastos.Length == 0 ? null : -gastos.Sum(m => m.Quantidade),
            compras == 0 ? null : compras, usos == 0 ? null : usos, areas,
            conclusoes.Count == 0 ? null : conclusoes.Count(o => o.Area is null), maiorAtividade, null,
            datas.Distinct().Count(),
            conclusoes.Count + movimentos.Count + resgates.Count, true,
            ["A ausência de registro não significa ausência da atividade.",
             "Conclusões usam a data de realização registrada no aplicativo e o fuso do período.",
             "Previstas, pendentes, atrasadas e taxa ainda dependem do histórico completo das ocorrências planejadas; não são estimadas pelas conclusões.",
             "Sequência e histórico manual de resultados ainda não estão disponíveis para esta análise.",
             "Histórico insuficiente para comparação conjunta de previstas, concluídas e taxa."]);
        return new(indicadores, SerieConclusoes.DasDatas(periodo, datas));
    }
}
