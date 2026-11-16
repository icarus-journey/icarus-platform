namespace Icarus.Application.Common.Jornada;

public static class CalculadorSequencia
{
    public static SequenciaDiaria Calcular(DadosJornada dados, DateTimeOffset agora)
    {
        if (!FusoHorario.EhValido(dados.FusoHorario))
            return new("fuso_nao_informado", null, null, null, null, []);

        var hoje = FusoHorario.DataLocal(agora, dados.FusoHorario);
        var apuracao = new ApuracaoRealizacoes(dados.Realizacoes, agora);
        var protegidas = (dados.DiasProtegidos ?? []).Where(d => d < hoje && apuracao.NoDia(d) == false).ToHashSet();
        var realizouHoje = apuracao.NoDia(hoje);
        var quantidade = apuracao.ContarAte(realizouHoje == true ? hoje : hoje.AddDays(-1), protegidas);
        if (realizouHoje is null) quantidade = null;
        var dias = Enumerable.Range(0, 7).Select(i => hoje.AddDays(i - 6))
            .Select(d => new DiaSequencia(d, apuracao.NoDia(d), protegidas.Contains(d) && apuracao.NoDia(d) != true)).ToArray();
        var estado = quantidade is null ? "historico_incompleto"
            : apuracao.SemRegistros ? "sem_registros" : "disponivel";
        return new(estado, quantidade, realizouHoje, hoje, dados.FusoHorario, dias);
    }
}
