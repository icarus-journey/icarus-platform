using Icarus.Application.Common;
using Icarus.Domain.Enums;

namespace Icarus.Application.UnitTests.Common;

public class GeradorDeDatasRecorrenciaTests
{
    [Fact(DisplayName = "Diária gera uma data para cada dia do intervalo, inclusive")]
    public void Gerar_Diaria_DeveGerarTodosOsDiasDoIntervalo()
    {
        var inicio = new DateOnly(2026, 3, 1);
        var fim = new DateOnly(2026, 3, 5);

        var datas = GeradorDeDatasRecorrencia.Gerar(inicio, fim, TipoRecorrencia.Diaria, "{}");

        Assert.Equal(5, datas.Count);
        Assert.Equal(inicio, datas[0]);
        Assert.Equal(fim, datas[^1]);
    }

    [Fact(DisplayName = "Semanal gera só as datas cujo dia da semana está na regra")]
    public void Gerar_Semanal_DeveGerarSoOsDiasConfigurados()
    {
        // 2026-03-02 é segunda-feira.
        var inicio = new DateOnly(2026, 3, 2);
        var fim = new DateOnly(2026, 3, 15); // duas semanas
        var regra = GeradorDeDatasRecorrencia.ConstruirRegra(TipoRecorrencia.Semanal, [DayOfWeek.Monday, DayOfWeek.Friday], 0);

        var datas = GeradorDeDatasRecorrencia.Gerar(inicio, fim, TipoRecorrencia.Semanal, regra);

        Assert.Equal(4, datas.Count); // 2 segundas + 2 sextas
        Assert.All(datas, d => Assert.Contains(d.DayOfWeek, new[] { DayOfWeek.Monday, DayOfWeek.Friday }));
    }

    [Fact(DisplayName = "Mensal gera a data no dia do mês configurado")]
    public void Gerar_Mensal_DeveGerarNoDiaDoMesConfigurado()
    {
        var inicio = new DateOnly(2026, 1, 1);
        var fim = new DateOnly(2026, 3, 31);
        var regra = GeradorDeDatasRecorrencia.ConstruirRegra(TipoRecorrencia.Mensal, null, 15);

        var datas = GeradorDeDatasRecorrencia.Gerar(inicio, fim, TipoRecorrencia.Mensal, regra);

        Assert.Equal([new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15), new DateOnly(2026, 3, 15)], datas);
    }

    [Fact(DisplayName = "Mensal com dia 31 usa o último dia em mês mais curto (RF-04.4)")]
    public void Gerar_Mensal_DeveUsarUltimoDiaEmMesCurto()
    {
        var inicio = new DateOnly(2026, 1, 1);
        var fim = new DateOnly(2026, 4, 30);
        var regra = GeradorDeDatasRecorrencia.ConstruirRegra(TipoRecorrencia.Mensal, null, 31);

        var datas = GeradorDeDatasRecorrencia.Gerar(inicio, fim, TipoRecorrencia.Mensal, regra);

        // 2026 não é bissexto: fevereiro tem 28 dias; abril tem 30.
        Assert.Equal(
            [new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 30)],
            datas);
    }

    [Fact(DisplayName = "Início depois do fim devolve lista vazia")]
    public void Gerar_DeveDevolverVazioQuandoInicioDepoisDoFim()
    {
        var datas = GeradorDeDatasRecorrencia.Gerar(new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 1), TipoRecorrencia.Diaria, "{}");

        Assert.Empty(datas);
    }
}
