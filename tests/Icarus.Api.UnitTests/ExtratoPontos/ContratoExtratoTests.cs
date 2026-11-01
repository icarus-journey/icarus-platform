using Icarus.Api.Contracts.ExtratoPontos;
using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.ConsultarExtratoPontos;
using Microsoft.AspNetCore.DataProtection;
using CursorDados = Icarus.Application.UseCases.ConsultarExtratoPontos.CursorExtratoPontos;
using Codec = Icarus.Api.Contracts.ExtratoPontos.CursorExtratoPontos;
namespace Icarus.Api.UnitTests.ExtratoPontos;

public sealed class ContratoExtratoTests
{
    [Fact]
    public void CursorCriptografaTitularFiltrosEAncoraERejeitaAdulteracao()
    {
        var codec = new Codec(new EphemeralDataProtectionProvider());
        var dados = new CursorDados(Guid.NewGuid(), new(null, null, true, NaturezaExtrato.Recebidos, 20), DateTimeOffset.UtcNow, 7, 20, DateTimeOffset.UtcNow, "UTC");
        var t = codec.Codificar(dados); Assert.Equal(dados, codec.Decodificar(t)); Assert.DoesNotContain(dados.UsuarioId.ToString(), t);
        Assert.Throws<ConsultaExtratoInvalidaException>(() => codec.Decodificar(t[..^10] + "adulterado"));
        Assert.Throws<ConsultaExtratoInvalidaException>(() => codec.Decodificar(new string('a', 4097)));
    }
    [Fact]
    public void RecusaFiltrosIncompletosInvertidosOuForaDosLimites()
    {
        var v = new ConsultarExtratoPontosRequestValidator();
        Assert.False(v.Validate(new ConsultarExtratoPontosRequest() { Inicio = new(2026, 1, 1) }).IsValid);
        Assert.False(v.Validate(new ConsultarExtratoPontosRequest() { Inicio = new(2026, 2, 1), Fim = new(2026, 1, 1) }).IsValid);
        Assert.False(v.Validate(new ConsultarExtratoPontosRequest() { Inicio = new(2000, 1, 1), Fim = new(2026, 1, 1) }).IsValid);
        Assert.False(v.Validate(new ConsultarExtratoPontosRequest() { Inicio = new(9999, 12, 1), Fim = DateOnly.MaxValue }).IsValid);
        Assert.False(v.Validate(new ConsultarExtratoPontosRequest() { Inicio = new(2026, 1, 1), Fim = new(2026, 2, 1), TodoHistorico = true }).IsValid);
        Assert.False(v.Validate(new ConsultarExtratoPontosRequest() { Limite = 51 }).IsValid); Assert.False(v.Validate(new ConsultarExtratoPontosRequest() { Natureza = (NaturezaExtrato)999 }).IsValid);
        Assert.True(v.Validate(new ConsultarExtratoPontosRequest() { TodoHistorico = true }).IsValid); Assert.True(v.Validate(new ConsultarExtratoPontosRequest()).IsValid);
    }
}
