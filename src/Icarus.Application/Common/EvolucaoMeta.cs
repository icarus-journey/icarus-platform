using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

public sealed record PontoEvolucaoMeta(DateOnly Data, decimal Valor, bool Revisado);
public sealed record EvolucaoMeta(string Estado, decimal? AvancoRegistrado, decimal? Faltante,
    string? FusoHorario, IReadOnlyList<PontoEvolucaoMeta> Pontos);

/// <summary>Adapta os snapshots reais, sem preencher datas ou converter unidades.</summary>
public static class AnalisadorEvolucaoMeta
{
    public static (EvolucaoMeta Evolucao, CenarioConquista Cenario) Analisar(Campanha campanha,
        IReadOnlyList<MedicaoCampanha> historico, DateTimeOffset agora)
    {
        (EvolucaoMeta, CenarioConquista) Ausencia(string motivo, decimal? faltante = null)
            => (new(motivo, null, faltante, null, []), new(false, motivo));

        if (campanha.ValorMensurado is not > 0 || string.IsNullOrWhiteSpace(campanha.Unidade))
            return Ausencia("meta_nao_definida");
        if (new[] { "ponto", "pontos", "xp" }.Contains(campanha.Unidade.Trim().ToLowerInvariant()))
            return Ausencia("unidade_sem_resultado_mensurado");

        var faltante = campanha.ValorAtual is { } valor ? Math.Max(0, campanha.ValorMensurado.Value - valor) : (decimal?)null;
        var ordenadas = historico.OrderBy(m => m.RegistradaEm).ThenBy(m => m.Id).ToArray();
        // Não une trechos separados por troca de alvo ou unidade, mesmo que depois voltem ao original.
        var trecho = ordenadas.Reverse().TakeWhile(m => m.ValorMensurado == campanha.ValorMensurado
            && string.Equals(m.Unidade.Trim(), campanha.Unidade.Trim(), StringComparison.OrdinalIgnoreCase)).Reverse().ToArray();
        if (trecho.Length == 0) return Ausencia(ordenadas.Length == 0 ? "historico_insuficiente" : "meta_alterada", faltante);
        var fusoId = trecho[^1].FusoHorario;
        if (!FusoHorario.EhValido(fusoId))
            return Ausencia("fuso_nao_comparavel", faltante);
        trecho = trecho.Reverse().TakeWhile(m => m.FusoHorario == fusoId).Reverse().ToArray();
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(fusoId!);
        var medicoes = trecho.Select(m => new MedicaoConquista(
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(m.RegistradaEm, fuso).DateTime),
            m.ValorAtual, m.ValorMensurado, m.Unidade, m.ValorAnterior > m.ValorAtual)).ToArray();
        var pontos = medicoes.GroupBy(m => m.Data).Select(g => new PontoEvolucaoMeta(g.Key, g.Last().Valor,
            g.Any(m => m.Correcao))).ToArray();
        var sincronizado = campanha.ValorAtual == trecho[^1].ValorAtual;
        var evolucao = new EvolucaoMeta(sincronizado ? "disponivel" : "aguardando_novo_registro",
            sincronizado && pontos.Length > 1 ? pontos[^1].Valor - pontos[0].Valor : null, faltante, fusoId, pontos);
        if (!sincronizado) return (evolucao, new(false, "aguardando_novo_registro"));
        if (campanha.Estado is not (EstadoCampanha.Planejada or EstadoCampanha.EmAndamento))
            return (evolucao, new(false, "campanha_encerrada"));
        if (faltante == 0) return (evolucao, new(false, "meta_alcancada"));
        if (trecho.Any(m => m.RegistradaEm > agora)) return (evolucao, new(false, "historico_desatualizado_ou_futuro"));
        // O gráfico resume o dia; o cenário mantém todos os cinco registros recentes, sem ocultar revisões/duplicatas.
        var cenario = CalculadorCenarioConquista.Calcular(medicoes.TakeLast(5),
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agora, fuso).DateTime));
        return (evolucao, cenario);
    }
}
