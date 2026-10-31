namespace Icarus.Application.Common.Jornada;

public static class CalculadorSequencia
{
    public static SequenciaDiaria Calcular(DadosJornada dados, DateTimeOffset agora)
    {
        if (!FusoHorario.EhValido(dados.FusoHorario))
            return new("fuso_nao_informado", null, null, null, null, []);

        var hoje = FusoHorario.DataLocal(agora, dados.FusoHorario);
        var realizadas = new HashSet<DateOnly>();
        var incertas = new List<(DateOnly? Inicio, DateOnly Fim)>();
        foreach (var registro in dados.Realizacoes)
        {
            if (registro.RealizadaEm > agora || registro.RegistradaEm > agora) continue;
            if (registro.RealizadaEm is { } instante && FusoHorario.EhValido(registro.FusoHorario))
            {
                realizadas.Add(FusoHorario.DataLocal(instante, registro.FusoHorario));
                continue;
            }
            // Sem fuso histórico, o instante pode pertencer a dois dias civis. Sem realização,
            // o registro só limita o último dia possível; nunca o substitui como realização.
            var limite = registro.RealizadaEm ?? registro.RegistradaEm ?? agora;
            incertas.Add((registro.RealizadaEm is { } realizada ? DataUtc(realizada.AddHours(-12)) : null,
                DataUtc(limite.AddHours(14))));
        }

        bool? Realizada(DateOnly dia) => realizadas.Contains(dia) ? true
            : incertas.Any(i => (i.Inicio is null || i.Inicio <= dia) && dia <= i.Fim) ? null : false;

        var realizouHoje = Realizada(hoje);
        var atual = realizouHoje == true ? hoje : hoje.AddDays(-1);
        int? quantidade = 0;
        while (Realizada(atual) == true)
        {
            quantidade++;
            if (atual == DateOnly.MinValue) break;
            atual = atual.AddDays(-1);
        }
        if (realizouHoje is null || Realizada(atual) is null) quantidade = null;
        var dias = Enumerable.Range(0, 7).Select(i => hoje.AddDays(i - 6))
            .Select(d => new DiaSequencia(d, Realizada(d))).ToArray();
        var estado = quantidade is null ? "historico_incompleto"
            : realizadas.Count == 0 && incertas.Count == 0 ? "sem_registros" : "disponivel";
        return new(estado, quantidade, realizouHoje, hoje, dados.FusoHorario, dias);
    }

    private static DateOnly DataUtc(DateTimeOffset instante) => DateOnly.FromDateTime(instante.UtcDateTime);
}
