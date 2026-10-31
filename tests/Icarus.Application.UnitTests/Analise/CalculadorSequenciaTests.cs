using System.Globalization;
using Icarus.Application.Common.Jornada;

namespace Icarus.Application.UnitTests.Analise;

public sealed class CalculadorSequenciaTests
{
    private static DateTimeOffset Instante(string valor) => DateTimeOffset.Parse(valor, CultureInfo.InvariantCulture);
    private static readonly DateTimeOffset Agora = Instante("2026-10-31T15:00:00Z");
    private static readonly string[] HorasRepetidas = ["2026-10-31T16:00:00Z", "2026-11-01T05:30:00Z", "2026-11-01T06:30:00Z"];
    private static RealizacaoSequencia Feita(string dia, string? fuso = "America/Sao_Paulo")
        => new(Instante(dia + "T12:00:00Z"), fuso, Agora);
    private static SequenciaDiaria Calcular(params RealizacaoSequencia[] registros)
        => CalculadorSequencia.Calcular(new(50, "America/Sao_Paulo", registros), Agora);

    [Fact]
    public void TresMissoesNoMesmoDiaContamComoUmDia()
    {
        var resultado = Calcular(Feita("2026-10-31"), Feita("2026-10-31"), Feita("2026-10-31"));
        Assert.Equal(1, resultado.DiasAtuais);
        Assert.True(resultado.RealizouHoje);
        Assert.Single(resultado.Dias, d => d.Realizada == true);
    }

    [Fact]
    public void HojeEmAbertoPreservaSequenciaAteOntem()
    {
        var resultado = Calcular(Feita("2026-10-29"), Feita("2026-10-30"));
        Assert.Equal(2, resultado.DiasAtuais);
        Assert.False(resultado.RealizouHoje);
        Assert.Equal(new DateOnly(2026, 10, 31), resultado.Hoje);
    }

    [Fact]
    public void RegistroRetroativoUneDiasSemContarDataDoRegistro()
    {
        Assert.Equal(1, Calcular(Feita("2026-10-29"), Feita("2026-10-31")).DiasAtuais);
        var resultado = Calcular(Feita("2026-10-29"), Feita("2026-10-31"), Feita("2026-10-30"));
        Assert.Equal(3, resultado.DiasAtuais);
        Assert.Equal(3, resultado.Dias.Count(d => d.Realizada == true));
        Assert.Equal(3, Calcular(Feita("2026-10-29"), Feita("2026-10-30"), Feita("2026-10-31"), Feita("2026-10-30")).DiasAtuais);
    }

    [Fact]
    public void RegistroDeOntemHojeNaoAcendeChamaDeHoje()
    {
        var resultado = Calcular(Feita("2026-10-30"));
        Assert.Equal(1, resultado.DiasAtuais);
        Assert.False(resultado.RealizouHoje);
    }

    [Fact]
    public void DiaEncerradoSemRealizacaoInterrompeSemApagarCalendario()
    {
        var resultado = Calcular(Feita("2026-10-28"), Feita("2026-10-29"));
        Assert.Equal(0, resultado.DiasAtuais);
        Assert.Equal(2, resultado.Dias.Count(d => d.Realizada == true));
        Assert.Equal("disponivel", resultado.Estado);
    }

    [Fact]
    public void SemHistoricoTemZeroRealESeteDatasDoServidor()
    {
        var resultado = Calcular();
        Assert.Equal("sem_registros", resultado.Estado);
        Assert.Equal(0, resultado.DiasAtuais);
        Assert.Equal(7, resultado.Dias.Count);
        Assert.Equal(new DateOnly(2026, 10, 25), resultado.Dias[0].Data);
        Assert.All(resultado.Dias, d => Assert.False(d.Realizada));
    }

    [Fact]
    public void FusoHistoricoNaoMudaComOFusoAtualDoPerfil()
    {
        var registro = new RealizacaoSequencia(Instante("2026-10-31T01:00:00Z"), "America/Sao_Paulo", Agora);
        var resultado = CalculadorSequencia.Calcular(new(0, "Asia/Tokyo", [registro, Feita("2026-10-31", "Asia/Tokyo")]), Agora);
        Assert.Equal(2, resultado.DiasAtuais);
        Assert.Equal("Asia/Tokyo", resultado.FusoHorario);
    }

    [Theory]
    [InlineData("2026-10-31T02:59:59Z", false)]
    [InlineData("2026-10-31T03:00:00Z", true)]
    public void FronteiraDoDiaUsaFusoDaRealizacao(string instante, bool hoje)
    {
        var resultado = Calcular(new RealizacaoSequencia(Instante(instante), "America/Sao_Paulo", Agora));
        Assert.Equal(1, resultado.DiasAtuais);
        Assert.Equal(hoje, resultado.RealizouHoje);
    }

    [Fact]
    public void HoraRepetidaNoFimDoHorarioDeVeraoContinuaUmDia()
    {
        var agora = Instante("2026-11-02T03:00:00Z");
        var registros = HorasRepetidas
            .Select(s => new RealizacaoSequencia(Instante(s), "America/New_York", agora)).ToArray();
        var resultado = CalculadorSequencia.Calcular(new(0, "America/New_York", registros), agora);
        Assert.Equal(new DateOnly(2026, 11, 1), resultado.Hoje);
        Assert.Equal(2, resultado.DiasAtuais);
    }

    [Theory]
    [InlineData("2026-01-01", "2025-12-31")]
    [InlineData("2024-03-01", "2024-02-29")]
    public void SequenciaAtravessaAnoEMesBissexto(string hoje, string ontem)
    {
        var agora = Instante(hoje + "T20:00:00Z");
        var registros = new[] { hoje, ontem }.Select(s => new RealizacaoSequencia(Instante(s + "T12:00:00Z"), "UTC", agora)).ToArray();
        Assert.Equal(2, CalculadorSequencia.Calcular(new(0, "UTC", registros), agora).DiasAtuais);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalido")]
    public void FusoDoPerfilAusenteNaoInventaZero(string? fuso)
    {
        var resultado = CalculadorSequencia.Calcular(new(0, fuso, []), Agora);
        Assert.Equal("fuso_nao_informado", resultado.Estado);
        Assert.Null(resultado.DiasAtuais);
        Assert.Null(resultado.RealizouHoje);
        Assert.Empty(resultado.Dias);
    }

    [Fact]
    public void LegadoRecenteSemFusoNaoUsaFusoAtualNemRegistroComoRealizacao()
    {
        var resultado = Calcular(Feita("2026-10-30", null), Feita("2026-10-31"));
        Assert.Equal("historico_incompleto", resultado.Estado);
        Assert.Null(resultado.DiasAtuais);
        Assert.True(resultado.RealizouHoje);
        Assert.Null(resultado.Dias.Single(d => d.Data == new DateOnly(2026, 10, 30)).Realizada);
    }

    [Fact]
    public void LegadoAntigoNaoBloqueiaSequenciaRecenteConhecida()
    {
        var resultado = Calcular(Feita("2026-10-10", null), Feita("2026-10-29"), Feita("2026-10-30"), Feita("2026-10-31"));
        Assert.Equal("disponivel", resultado.Estado);
        Assert.Equal(3, resultado.DiasAtuais);
    }

    [Fact]
    public void LegadoSemRealizacaoNaoViraDiaConcluidoPeloRegistro()
    {
        var resultado = Calcular(new RealizacaoSequencia(null, null, Agora));
        Assert.Equal("historico_incompleto", resultado.Estado);
        Assert.Null(resultado.RealizouHoje);
        Assert.Null(resultado.DiasAtuais);
        Assert.DoesNotContain(resultado.Dias, d => d.Realizada == true);
    }

    [Fact]
    public void RegistroFuturoNaoEntraNaContagem()
    {
        var resultado = Calcular(new RealizacaoSequencia(Agora.AddMinutes(1), "UTC", Agora));
        Assert.Equal(0, resultado.DiasAtuais);
        Assert.Equal("sem_registros", resultado.Estado);
    }
}
