namespace Icarus.Application.Common;

public sealed record MedicaoConquista(DateOnly Data, decimal Valor, decimal Alvo, string Unidade, bool Correcao = false);

public sealed record CenarioConquista(bool Disponivel, string Motivo, decimal? RitmoSemanal = null,
    decimal? Faltante = null, int? SemanasAproximadas = null);

/// <summary>Cenário aritmético condicional; não estima probabilidade ou efeito de hábitos.</summary>
public static class CalculadorCenarioConquista
{
    public static CenarioConquista Calcular(IEnumerable<MedicaoConquista> historico, DateOnly referencia)
    {
        var medicoes = historico.OrderBy(m => m.Data).ToArray();
        if (medicoes.Length < 5)
            return new(false, "historico_insuficiente");
        if (medicoes.Select(m => m.Data).Distinct().Count() != medicoes.Length)
            return new(false, "datas_duplicadas");

        var janela = medicoes.TakeLast(5).ToArray();
        var atual = janela[^1];
        if (atual.Data > referencia || referencia.DayNumber - atual.Data.DayNumber > 7)
            return new(false, "historico_desatualizado_ou_futuro");
        if (string.IsNullOrWhiteSpace(atual.Unidade)
            || new[] { "ponto", "pontos", "xp" }.Contains(atual.Unidade.Trim().ToLowerInvariant()))
            return new(false, "unidade_sem_resultado_mensurado");
        if (janela.Any(m => m.Correcao || m.Valor < 0 || m.Alvo <= 0
            || m.Alvo != atual.Alvo || !string.Equals(m.Unidade?.Trim(), atual.Unidade.Trim(), StringComparison.OrdinalIgnoreCase)))
            return new(false, "medicoes_nao_comparaveis");

        var incrementos = new decimal[4];
        for (var indice = 1; indice < janela.Length; indice++)
        {
            if (janela[indice].Data.DayNumber - janela[indice - 1].Data.DayNumber != 7)
                return new(false, "janela_semanal_incompleta");
            incrementos[indice - 1] = janela[indice].Valor - janela[indice - 1].Valor;
        }

        var ritmo = incrementos.Average();
        if (incrementos.Any(valor => valor < 0) || ritmo <= 0 || incrementos.Max() > 2 * ritmo)
            return new(false, "ritmo_sem_estabilidade_para_projecao");
        var faltante = Math.Max(0, atual.Alvo - atual.Valor);
        var semanas = decimal.Ceiling(faltante / ritmo);
        if (semanas > int.MaxValue)
            return new(false, "projecao_fora_dos_limites");
        return new(true, "cenario_condicional_se_mantiver_o_ritmo", ritmo, faltante, (int)semanas);
    }
}
