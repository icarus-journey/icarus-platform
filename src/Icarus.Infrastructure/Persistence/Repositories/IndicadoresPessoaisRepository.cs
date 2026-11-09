using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.Interfaces;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class IndicadoresPessoaisRepository(IcarusDbContext contexto, TimeProvider? relogio = null) : IIndicadoresPessoaisRepository
{
    public async Task<IndicadoresPessoais> CalcularAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct)
        => (await CalcularAsync(usuarioId, periodo, false, ct)).Indicadores;

    public async Task<DadosRelatorioSemanal> CalcularRelatorioAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct)
    {
        await using var leitura = contexto.Database.CurrentTransaction is null
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var atividade = (await CalcularAsync(usuarioId, periodo, false, ct)).Indicadores;
        var periodos = Enumerable.Range(0, 5)
            .Select(i => new PeriodoAnalitico(periodo.Inicio.AddDays(-7 * i), periodo.Fim.AddDays(-7 * i), periodo.FusoHorario)).ToArray();
        var planejamentos = await LeitorPlanejamentoAnalitico.LerPeriodosAsync(contexto, usuarioId, periodos, ct);
        var semanas = periodos.Select((semana, i) => CalculadorComparacaoSemanal.DaAdesao(semana, planejamentos[i].Resumo)).ToArray();
        var registros = planejamentos[0].Registros;
        var comparacao = CalculadorComparacaoSemanal.Calcular(semanas[0], semanas.Skip(1).ToArray());
        var idsAtividade = await contexto.Ocorrencias.AsNoTracking().Where(o => o.Missao.UsuarioId == usuarioId
            && o.RealizadaEm >= periodo.InicioUtc && o.RealizadaEm < periodo.FimExclusivoUtc
            && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(o => o.Id).ToListAsync(ct);
        var limitacoes = atividade.Limitacoes.Where(l => !l.StartsWith("Histórico insuficiente", StringComparison.Ordinal)).ToList();
        if (comparacao.HistoricoInsuficiente)
            limitacoes.Add("Histórico insuficiente para a comparação completa com as semanas anteriores.");
        var dados = atividade with
        {
            HistoricoInsuficienteComparacao = comparacao.HistoricoInsuficiente,
            QuantidadeRegistros = atividade.QuantidadeRegistros + registros.Except(idsAtividade).Count(),
            Limitacoes = limitacoes
        };
        if (leitura is not null) await leitura.CommitAsync(ct);
        return new(dados, comparacao);
    }

    public async Task<ProgressoCalculado> CalcularProgressoAsync(Guid usuarioId, PeriodoAnalitico periodo, CancellationToken ct)
        => await CalcularAsync(usuarioId, periodo, true, ct);

    private async Task<ProgressoCalculado> CalcularAsync(Guid usuarioId, PeriodoAnalitico periodo, bool incluirPlanejamento, CancellationToken ct)
    {
        await using var leitura = contexto.Database.CurrentTransaction is null
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var agora = (relogio ?? TimeProvider.System).GetUtcNow();
        var inicio = periodo.InicioUtc; var fim = periodo.FimExclusivoUtc;
        var conclusoes = await contexto.Ocorrencias.AsNoTracking()
            .Where(o => o.Missao.UsuarioId == usuarioId && o.RealizadaEm >= inicio && o.RealizadaEm < fim
                && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .Select(o => new
            {
                o.Id,
                o.RealizadaEm,
                o.Status,
                o.Missao.CampanhaId,
                Area = o.Missao.Campanha != null ? (AreaDaVida?)o.Missao.Campanha.AreaDaVida : null
            })
            .ToListAsync(ct);
        var movimentos = await contexto.MovimentacoesPontos.AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId && m.CriadoEm >= inicio && m.CriadoEm < fim)
            .Select(m => new { m.Tipo, m.Quantidade }).ToListAsync(ct);
        var resgates = await contexto.Set<Icarus.Domain.Entities.ResgateItem>().AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId && ((r.ResgatadoEm >= inicio && r.ResgatadoEm < fim)
                || (r.UtilizadoEm >= inicio && r.UtilizadoEm < fim)))
            .Select(r => new
            {
                r.ResgatadoEm,
                r.UtilizadoEm,
                r.CompensacaoOrigemId,
                UsoCompensado = contexto.Set<Icarus.Domain.Entities.ConsumoProtecao>().Any(c => c.ResgateId == r.Id && c.CompensadoEm != null)
            }).ToListAsync(ct);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(periodo.FusoHorario);
        var areas = conclusoes.Where(o => o.Area is not null).GroupBy(o => o.Area!.Value)
            .Select(g => new ConclusoesPorArea(CatalogoAreasAnaliticas.Areas.Single(a => a.Area == g.Key).Codigo, g.Count()))
            .OrderBy(a => a.AreaDaVida, StringComparer.Ordinal).ToArray();
        var ganhos = movimentos.Where(m => m.Quantidade > 0 && (m.Tipo is TipoMovimentacaoPontos.GanhoMissao or TipoMovimentacaoPontos.Bonus)).ToArray();
        var gastos = movimentos.Where(m => m.Quantidade < 0 && m.Tipo == TipoMovimentacaoPontos.CompraItem).ToArray();
        var compras = resgates.Count(r => r.CompensacaoOrigemId == null && r.ResgatadoEm >= inicio && r.ResgatadoEm < fim);
        var usos = resgates.Count(r => !r.UsoCompensado && r.UtilizadoEm >= inicio && r.UtilizadoEm < fim);
        var registrosLoja = resgates.Count(r => (r.CompensacaoOrigemId == null && r.ResgatadoEm >= inicio && r.ResgatadoEm < fim)
            || (!r.UsoCompensado && r.UtilizadoEm >= inicio && r.UtilizadoEm < fim));
        var maiorAtividade = conclusoes.Where(o => o.CampanhaId is not null).GroupBy(o => o.CampanhaId)
            .Select(g => (int?)g.Count()).OrderDescending().FirstOrDefault();
        var datas = conclusoes.Select(o => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(o.RealizadaEm!.Value, fuso).DateTime)).ToArray();
        ResumoAdesao? adesao = null;
        ComparacaoSemanal? comparacao = null;
        var registrosPlanejamento = 0;
        if (incluirPlanejamento)
        {
            var semanal = periodo.Fim.DayNumber - periodo.Inicio.DayNumber == 6 && periodo.Inicio.DayOfWeek == DayOfWeek.Monday;
            var periodos = Enumerable.Range(0, semanal ? 5 : 1).Select(i => new PeriodoAnalitico(
                periodo.Inicio.AddDays(-7 * i), periodo.Fim.AddDays(-7 * i), periodo.FusoHorario)).ToArray();
            var planejamentos = await LeitorPlanejamentoAnalitico.LerPeriodosAsync(contexto, usuarioId, periodos, ct);
            var planejamento = planejamentos[0];
            if (semanal)
            {
                var semanas = periodos.Select((p, i) => CalculadorComparacaoSemanal.DaAdesao(p, planejamentos[i].Resumo)).ToArray();
                comparacao = CalculadorComparacaoSemanal.Calcular(semanas[0], semanas.Skip(1).ToArray());
            }
            adesao = planejamento.Resumo;
            registrosPlanejamento = planejamento.Registros.Except(conclusoes.Select(o => o.Id)).Count();
        }
        var disponivel = adesao?.Estado is "disponivel" or "sem_planejamento";
        var limitacoes = new List<string>
        {
            "A ausência de registro não significa ausência da atividade.",
            "Conclusões usam a data de realização registrada no aplicativo e o fuso do período.",
            "A adesão considera o planejamento vigente ao fechar o período e o que foi realizado até esse fechamento; o total de atividades feitas usa a data da realização.",
            incluirPlanejamento
                ? "Objetivos e metas mostram a situação atual; atividade e avanço mensurado respeitam o período."
                : "Sequência e histórico manual de resultados ainda não estão disponíveis para esta análise.",
            "Histórico insuficiente para comparação conjunta de previstas, concluídas e taxa."
        };
        if (adesao?.Estado == "historico_parcial")
            limitacoes.Add("Parte do planejamento antigo não tem versões ou há lacunas na janela recorrente; a taxa não é estimada pelos registros de conclusão.");
        var indicadores = new IndicadoresPessoais(disponivel ? adesao!.PrevistasRegistradas : null,
            conclusoes.Count == 0 ? null : conclusoes.Count, disponivel ? adesao!.TaxaConclusao : null,
            disponivel ? adesao!.Pendentes : null, disponivel ? adesao!.Atrasadas : null,
            conclusoes.Count == 0 ? null : conclusoes.Count(o => o.Status == StatusOcorrencia.ConcluidaComAtraso), null,
            ganhos.Length == 0 ? null : ganhos.Sum(m => m.Quantidade), gastos.Length == 0 ? null : -gastos.Sum(m => m.Quantidade),
            compras == 0 ? null : compras, usos == 0 ? null : usos, areas,
            conclusoes.Count == 0 ? null : conclusoes.Count(o => o.Area is null), maiorAtividade, null,
            datas.Distinct().Count(),
            conclusoes.Count + registrosPlanejamento + movimentos.Count + registrosLoja, true, limitacoes);
        var objetivos = incluirPlanejamento ? await LeitorPanoramaObjetivos.LerAsync(contexto, usuarioId, periodo, agora, ct) : null;
        if (leitura is not null) await leitura.CommitAsync(ct);
        return new(indicadores, SerieConclusoes.DasDatas(periodo, datas), adesao, objetivos, comparacao);
    }
}
