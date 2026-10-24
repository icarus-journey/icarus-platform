namespace Icarus.Application.Common.Analise;

public static class SelecionadorReferenciasPublicas
{
    public static string? FaixaDaIdade(int idade) => idade switch
    {
        < 15 => null, 15 => "15", <= 17 => "16–17", <= 24 => "18–24", <= 28 => "25–28",
        <= 34 => "29–34", <= 49 => "35–49", <= 64 => "50–64", _ => "65+"
    };

    public static ReferenciaPublica? Selecionar(IReadOnlyList<ReferenciaPublica> referencias, string indicador,
        string medida, int idade, bool? estuda = null, bool? trabalha = null, string tipoDia = "todos")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(idade);
        if (tipoDia is not ("todos" or "segunda_sexta" or "sabado_domingo"))
            throw new ArgumentException("Tipo de dia inválido.", nameof(tipoDia));
        var vigitel = indicador.StartsWith("vigitel_", StringComparison.Ordinal);
        if (idade < (vigitel ? 18 : 15)) return null;
        var folha = FaixaDaIdade(idade)!;
        var faixas = new List<string> { folha };
        faixas.AddRange(idade < 18 ? ["15–17"] : idade <= 28 ? ["18–28", "18+"] : ["18+"]);
        var semFiltros = new FiltrosReferencia();
        var filtros = vigitel ? [semFiltros] : new[] {
            new FiltrosReferencia(idade < 50 ? estuda : null, trabalha),
            new FiltrosReferencia(null, trabalha), new FiltrosReferencia(idade < 50 ? estuda : null), semFiltros };
        foreach (var tipo in tipoDia == "todos" ? new[] { "todos" } : [tipoDia, "todos"])
        foreach (var faixa in faixas)
        foreach (var filtro in filtros.Distinct())
        {
            if (faixa == "18+" && filtro.Estuda is not null) continue;
            var referencia = referencias.FirstOrDefault(r => r.Indicador == indicador && r.Medida == medida
                && r.TipoDia == tipo && r.FaixaEtaria == faixa && r.Filtros == filtro);
            if (referencia is not null) return referencia;
        }
        return null;
    }
}
