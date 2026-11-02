using System.Globalization;
using Icarus.Application.Common;
using Icarus.Application.Exceptions;

namespace Icarus.Application.UnitTests.Common;

public sealed class RealizacaoMissaoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 31, 1, 10, 30, TimeSpan.Zero);

    [Theory]
    [InlineData("America/Sao_Paulo", "America/Sao_Paulo")]
    [InlineData(null, "UTC")]
    [InlineData("legado/invalido", "UTC")]
    public void AgoraVemDoRelogioDaApiSemCongelarNaAbertura(string? fuso, string esperado)
    {
        var r = RealizacaoMissao.Resolver(null, null, Agora, fuso);
        Assert.Equal(Agora, r.Instante); Assert.Equal(esperado, r.FusoHorario);
    }

    [Theory]
    [InlineData("America/Sao_Paulo", 30, 20, "2026-10-30T23:00:00Z")]
    [InlineData("UTC", 30, 20, "2026-10-30T20:00:00Z")]
    [InlineData("Asia/Tokyo", 31, 8, "2026-10-30T23:00:00Z")]
    public void ConverteDataCivilDoPerfilEmInstanteUtc(string fuso, int dia, int hora, string esperado)
        => Assert.Equal(DateTimeOffset.Parse(esperado, CultureInfo.InvariantCulture), RealizacaoMissao.Resolver(new(2026, 10, dia), new(hora, 0), Agora, fuso).Instante);

    [Fact]
    public void NaoAceitaDataOuHorarioIsolado()
    {
        Assert.Equal("realizacao_incompleta", Assert.Throws<EdicaoMissaoException>(() => RealizacaoMissao.Resolver(new(2026, 10, 30), null, Agora, "UTC")).Codigo);
        Assert.Equal("realizacao_incompleta", Assert.Throws<EdicaoMissaoException>(() => RealizacaoMissao.Resolver(null, new(20, 0), Agora, "UTC")).Codigo);
    }

    [Theory]
    [InlineData("America/Sao_Paulo", 30, 22, 11)]
    [InlineData("UTC", 31, 1, 11)]
    [InlineData("Asia/Tokyo", 31, 10, 11)]
    public void RejeitaFuturoNoMesmoDiaLocal(string fuso, int dia, int hora, int minuto)
        => Assert.Equal("realizacao_futura", Assert.Throws<EdicaoMissaoException>(() => RealizacaoMissao.Resolver(new(2026, 10, dia), new(hora, minuto), Agora, fuso)).Codigo);

    [Theory]
    [InlineData(3, 8, 2, "horario_inexistente")]
    [InlineData(11, 1, 1, "horario_ambiguo")]
    public void RejeitaHorarioCivilInexistenteOuAmbiguoSemAdivinharDeslocamento(int mes, int dia, int hora, string codigo)
        => Assert.Equal(codigo, Assert.Throws<EdicaoMissaoException>(() => RealizacaoMissao.Resolver(new(2026, mes, dia), new(hora, 30), new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero), "America/New_York")).Codigo);
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetornoInicialJaUsaPrecisaoPersistidaSemArredondarParaOFuturo(bool manual)
    {
        var r = RealizacaoMissao.Resolver(manual ? new DateOnly(2026, 10, 30) : null,
            manual ? new TimeOnly(20, 0).Add(TimeSpan.FromTicks(9)) : null, Agora.AddTicks(9), "UTC");
        Assert.Equal(0, r.Instante.Ticks % 10);
        Assert.True(r.Instante <= Agora.AddTicks(9));
    }

}
