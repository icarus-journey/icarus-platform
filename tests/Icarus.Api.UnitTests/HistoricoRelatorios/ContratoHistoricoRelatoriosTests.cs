using Icarus.Api.Contracts.HistoricoRelatorios;
using Icarus.Application.Exceptions;
using Microsoft.AspNetCore.DataProtection;
using CursorDados = Icarus.Application.UseCases.ConsultarHistoricoRelatorios.CursorHistoricoRelatorios;
namespace Icarus.Api.UnitTests.HistoricoRelatorios;

public sealed class ContratoHistoricoRelatoriosTests
{
    [Fact]
    public void CursorProtegeEscopoERejeitaAdulteracao()
    {
        var codec = new CursorHistoricoRelatorios(new EphemeralDataProtectionProvider());
        var dados = new CursorDados(Guid.NewGuid(), 20, new(2026, 10, 12), 10, 20, DateTimeOffset.UtcNow);
        var token = codec.Codificar(dados); Assert.Equal(dados, codec.Decodificar(token)); Assert.DoesNotContain(dados.UsuarioId.ToString(), token);
        Assert.Throws<ConsultaHistoricoRelatoriosInvalidaException>(() => codec.Decodificar(token[..^10] + "adulterado"));
        Assert.Throws<ConsultaHistoricoRelatoriosInvalidaException>(() => codec.Decodificar(new string('a', 4097)));
    }
    [Fact]
    public void ValidaTamanhoDaPaginaEDoCursor()
    {
        var v = new ConsultaHistoricoRelatoriosRequestValidator();
        Assert.True(v.Validate(new ConsultaHistoricoRelatoriosRequest()).IsValid);
        Assert.False(v.Validate(new ConsultaHistoricoRelatoriosRequest(0)).IsValid);
        Assert.False(v.Validate(new ConsultaHistoricoRelatoriosRequest(51)).IsValid);
        Assert.False(v.Validate(new ConsultaHistoricoRelatoriosRequest(Cursor: new string('a', 4097))).IsValid);
    }
}
