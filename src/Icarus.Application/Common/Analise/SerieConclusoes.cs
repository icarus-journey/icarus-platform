namespace Icarus.Application.Common.Analise;

public sealed record PontoSerieConclusoes(DateOnly Inicio, DateOnly Fim, int Concluidas);
public sealed record SerieConclusoes(string Granularidade, IReadOnlyList<PontoSerieConclusoes> Pontos)
{
    public static SerieConclusoes? DasDatas(PeriodoAnalitico periodo, IReadOnlyList<DateOnly> datas)
    {
        if (datas.Count == 0) return null;
        var dias = periodo.Fim.DayNumber - periodo.Inicio.DayNumber + 1;
        var granularidade = dias <= 7 ? "dia" : dias <= 31 ? "semana" : "mes";
        var pontos = new List<PontoSerieConclusoes>();
        for (var inicio = periodo.Inicio; inicio <= periodo.Fim;)
        {
            var proximo = granularidade switch
            {
                "dia" => inicio.AddDays(1),
                "semana" => inicio.AddDays(7),
                _ => inicio.AddMonths(1)
            };
            var fim = proximo.AddDays(-1) < periodo.Fim ? proximo.AddDays(-1) : periodo.Fim;
            pontos.Add(new(inicio, fim, datas.Count(d => d >= inicio && d <= fim)));
            inicio = proximo;
        }
        return new(granularidade, pontos);
    }
}

public sealed record ProgressoCalculado(IndicadoresPessoais Indicadores, SerieConclusoes? Evolucao);
