using System.Globalization;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common.Analise;

public static class CalculadorPanoramaObjetivos
{
    private static readonly StringComparer OrdemTitulos = StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), true);

    public static PanoramaObjetivos Calcular(IReadOnlyList<Epico> epicos, IReadOnlyList<AtividadeObjetivo> registros,
        IReadOnlyList<MedicaoCampanha> medicoes, PeriodoAnalitico periodo, DateTimeOffset agora)
    {
        var hoje = FusoHorario.DataLocal(agora, periodo.FusoHorario);
        var janela = new PeriodoAnalitico(hoje.AddDays(-6), hoje, periodo.FusoHorario);
        var visiveis = epicos.Where(e => e.Estado != EstadoEpico.Excluido).ToArray();
        var campanhas = visiveis.SelectMany(e => e.Campanhas).Where(c => c.Estado != EstadoCampanha.Excluida)
            .OrderBy(c => c.Titulo, OrdemTitulos).ThenBy(c => c.Id).Select(c => new CampanhaNoPanorama(
                c.Id, c.EpicoId, c.Titulo, c.AreaDaVida, c.Estado, c.PrazoFim,
                Atividade(registros.Where(r => r.CampanhaId == c.Id), periodo, janela, agora),
                Resultado(c, medicoes.Where(m => m.CampanhaId == c.Id).ToArray(), periodo, agora))).ToArray();
        var resumosEpicos = visiveis.OrderBy(e => e.Titulo, OrdemTitulos).ThenBy(e => e.Id).Select(e => new EpicoNoPanorama(
            e.Id, e.Titulo, e.Estado, new(
                e.Campanhas.Count(c => c.Estado == EstadoCampanha.Planejada),
                e.Campanhas.Count(c => c.Estado == EstadoCampanha.EmAndamento),
                e.Campanhas.Count(c => c.Estado == EstadoCampanha.Concluida)),
            Atividade(registros.Where(r => r.EpicoId == e.Id), periodo, janela, agora))).ToArray();
        var ativas = campanhas.Where(c => c.Estado == EstadoCampanha.EmAndamento).ToArray();
        var recentesIncertas = ativas.Any(c => c.Atividade.ConcluidasNosUltimosSeteDias is null);
        var destaque = recentesIncertas ? null : ativas.Where(c => c.Atividade.ConcluidasNosUltimosSeteDias > 0)
            .OrderByDescending(c => c.Atividade.ConcluidasNosUltimosSeteDias).ThenBy(c => c.Titulo, OrdemTitulos).ThenBy(c => c.Id).FirstOrDefault();
        return new(agora, janela, ativas.Length, resumosEpicos.Count(e => e.Estado == EstadoEpico.EmAndamento),
            recentesIncertas ? null : ativas.Count(c => c.Atividade.ConcluidasNosUltimosSeteDias == 0),
            recentesIncertas ? "historico_incompleto" : destaque is null ? "sem_realizacao_recente" : "disponivel",
            destaque?.Id, campanhas, resumosEpicos);
    }

    private static AtividadeNoObjetivo Atividade(IEnumerable<AtividadeObjetivo> registros,
        PeriodoAnalitico periodo, PeriodoAnalitico janela, DateTimeOffset agora)
    {
        var lista = registros.Where(r => (r.RealizadaEm <= agora || r.RealizadaEm is null) && (r.RegistradaEm <= agora || r.RegistradaEm is null)).ToArray();
        var noPeriodo = lista.Where(r => r.RealizadaEm >= periodo.InicioUtc && r.RealizadaEm < periodo.FimExclusivoUtc).ToArray();
        var recentes = lista.Count(r => r.RealizadaEm >= janela.InicioUtc);
        var datas = noPeriodo.Select(r => FusoHorario.DataLocal(r.RealizadaEm!.Value, periodo.FusoHorario)).Distinct().Count();
        bool IncertoDesde(DateTimeOffset inicio) => lista.Any(r => r.RealizadaEm is null
            && (r.RegistradaEm is null || r.RegistradaEm >= inicio));
        return new(IncertoDesde(periodo.InicioUtc) ? null : noPeriodo.Length, noPeriodo.Length, datas,
            IncertoDesde(janela.InicioUtc) ? null : recentes, lista.Max(r => r.RealizadaEm));
    }

    private static ResultadoNoObjetivo Resultado(Campanha campanha, IReadOnlyList<MedicaoCampanha> historico,
        PeriodoAnalitico periodo, DateTimeOffset agora)
    {
        ResultadoNoObjetivo Ausencia(string estado) => new(estado, null, null, null, 0, 0, null,
            new(estado, null, null, periodo.FusoHorario, []), new(false, estado));
        if (campanha.ValorMensurado is not > 0 || string.IsNullOrWhiteSpace(campanha.Unidade)) return Ausencia("meta_nao_definida");
        if (new[] { "ponto", "pontos", "xp" }.Contains(campanha.Unidade.Trim().ToLowerInvariant())) return Ausencia("unidade_sem_resultado_mensurado");
        var analiseAtual = AnalisadorEvolucaoMeta.Analisar(campanha, historico, agora);
        var ordenadas = historico.Where(m => m.RegistradaEm <= agora).OrderBy(m => m.RegistradaEm).ThenBy(m => m.Id).ToArray();
        var trecho = ordenadas.Reverse().TakeWhile(m => m.ValorMensurado == campanha.ValorMensurado
            && string.Equals(m.Unidade.Trim(), campanha.Unidade.Trim(), StringComparison.OrdinalIgnoreCase)).Reverse().ToArray();
        var fuso = trecho.LastOrDefault()?.FusoHorario;
        var comparaveis = FusoHorario.EhValido(fuso) ? trecho.Reverse().TakeWhile(m => m.FusoHorario == fuso).Reverse().ToArray() : [];
        var periodoObservado = ordenadas.Where(m => m.RegistradaEm >= periodo.InicioUtc && m.RegistradaEm < periodo.FimExclusivoUtc).ToArray();
        var dentro = comparaveis.Where(m => m.RegistradaEm >= periodo.InicioUtc && m.RegistradaEm < periodo.FimExclusivoUtc).ToArray();
        var anterior = comparaveis.LastOrDefault(m => m.RegistradaEm < periodo.InicioUtc);
        var baseAvanco = dentro.Length == 0 ? null : anterior is not null ? "registro_anterior" : dentro.Length > 1 ? "primeiro_registro_do_periodo" : null;
        var avanco = baseAvanco is null ? null : (decimal?)(dentro[^1].ValorAtual - (anterior?.ValorAtual ?? dentro[0].ValorAtual));
        var pontos = dentro.GroupBy(m => FusoHorario.DataLocal(m.RegistradaEm, periodo.FusoHorario))
            .Select(g => new PontoEvolucaoMeta(g.Key, g.Last().ValorAtual, g.Any(m => m.ValorAnterior > m.ValorAtual))).ToArray();
        var faltante = campanha.ValorAtual is { } atual ? Math.Max(0, campanha.ValorMensurado.Value - atual) : (decimal?)null;
        var estado = pontos.Length > 0 ? "disponivel" : periodoObservado.Length > 0 ? "medicoes_nao_comparaveis" : "sem_registros_no_periodo";
        return new(estado, campanha.ValorAtual, campanha.ValorMensurado, campanha.Unidade, periodoObservado.Length,
            periodoObservado.Select(m => FusoHorario.DataLocal(m.RegistradaEm, periodo.FusoHorario)).Distinct().Count(), baseAvanco,
            new(estado, avanco, faltante, periodo.FusoHorario, pontos), analiseAtual.Cenario);
    }
}
