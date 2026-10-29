using System.Globalization;
using System.Text.Json;
using Icarus.Application.Common.Analise;

namespace Icarus.Application.UnitTests.Analise;

public sealed class PeriodoAnaliticoTests
{
    [Theory]
    [InlineData("semanaAnterior", "2026-10-12", "2026-10-18")]
    [InlineData("mesAnterior", "2026-09-01", "2026-09-30")]
    [InlineData("seisMeses", "2026-04-01", "2026-09-30")]
    public void UsaPeriodosCivisCompletos(string periodo, string inicio, string fim)
    {
        var resultado = PeriodoAnalitico.Encerrado(periodo, "America/Sao_Paulo", DateTimeOffset.Parse("2026-10-20T12:00:00Z", CultureInfo.InvariantCulture));
        Assert.Equal(DateOnly.Parse(inicio, CultureInfo.InvariantCulture), resultado.Inicio);
        Assert.Equal(DateOnly.Parse(fim, CultureInfo.InvariantCulture), resultado.Fim);
        Assert.DoesNotContain("inicioUtc", JsonSerializer.Serialize(resultado, JsonSerializerOptions.Web));
    }

    [Fact]
    public void DomingoLocalNaoAntecipaFechamentoPorqueUtcJaEstaNaSegunda()
    {
        var resultado = PeriodoAnalitico.Encerrado("semanaAnterior", "America/Sao_Paulo", DateTimeOffset.Parse("2026-10-19T02:59:00Z", CultureInfo.InvariantCulture));
        Assert.Equal(new DateOnly(2026, 10, 5), resultado.Inicio);
        Assert.Equal(DateTimeOffset.Parse("2026-10-12T03:00:00Z", CultureInfo.InvariantCulture), resultado.FimExclusivoUtc);
    }

    [Fact]
    public void MudancaDeHorarioNaoPresumeSemanaDe168Horas()
    {
        var periodo = new PeriodoAnalitico(new(2026, 3, 2), new(2026, 3, 8), "America/New_York");
        Assert.Equal(167, (periodo.FimExclusivoUtc - periodo.InicioUtc).TotalHours);
        var meiaNoiteInexistente = new PeriodoAnalitico(new(2018, 11, 4), new(2018, 11, 4), "America/Sao_Paulo");
        Assert.Equal(23, (meiaNoiteInexistente.FimExclusivoUtc - meiaNoiteInexistente.InicioUtc).TotalHours);
    }
}
