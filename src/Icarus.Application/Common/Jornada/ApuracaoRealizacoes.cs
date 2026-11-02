namespace Icarus.Application.Common.Jornada;

/// <summary>Apura dias conhecidos e preserva a incerteza dos registros legados.</summary>
public sealed class ApuracaoRealizacoes
{
    private readonly HashSet<DateOnly> realizadas = [];
    private readonly List<(DateOnly? Inicio, DateOnly Fim)> incertas = [];
    public bool SemRegistros => realizadas.Count == 0 && incertas.Count == 0;

    public ApuracaoRealizacoes(IEnumerable<RealizacaoSequencia> registros, DateTimeOffset agora)
    {
        foreach (var registro in registros)
        {
            if (registro.RealizadaEm > agora || registro.RegistradaEm > agora) continue;
            if (registro.RealizadaEm is { } instante && FusoHorario.EhValido(registro.FusoHorario))
            {
                realizadas.Add(FusoHorario.DataLocal(instante, registro.FusoHorario));
                continue;
            }
            var limite = registro.RealizadaEm ?? registro.RegistradaEm ?? agora;
            incertas.Add((registro.RealizadaEm is { } realizada ? DataUtc(realizada.AddHours(-12)) : null,
                DataUtc(limite.AddHours(14))));
        }
    }

    public bool? NoDia(DateOnly dia) => realizadas.Contains(dia) ? true
        : incertas.Any(i => (i.Inicio is null || i.Inicio <= dia) && dia <= i.Fim) ? null : false;

    public int? ContarAte(DateOnly dia, IReadOnlySet<DateOnly>? protegidas = null)
    {
        var quantidade = 0;
        while (NoDia(dia) == true || protegidas?.Contains(dia) == true)
        {
            if (NoDia(dia) == true) quantidade++;
            if (dia == DateOnly.MinValue) return quantidade;
            dia = dia.AddDays(-1);
        }
        return NoDia(dia) is null ? null : quantidade;
    }

    private static DateOnly DataUtc(DateTimeOffset instante) => DateOnly.FromDateTime(instante.UtcDateTime);
}
