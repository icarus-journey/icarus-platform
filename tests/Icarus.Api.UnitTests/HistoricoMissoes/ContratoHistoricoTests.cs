using Icarus.Api.Contracts.HistoricoMissoes;
using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.ConsultarHistoricoMissoes;
using Microsoft.AspNetCore.DataProtection;
using CursorDados = Icarus.Application.UseCases.ConsultarHistoricoMissoes.CursorHistoricoMissoes;
using Codec = Icarus.Api.Contracts.HistoricoMissoes.CursorHistoricoMissoes;
namespace Icarus.Api.UnitTests.HistoricoMissoes;

public sealed class ContratoHistoricoTests
{
    [Fact]
    public void CursorProtegeTitularFiltrosEAncoraContraAlteracao()
    {
        var codec = new Codec(new EphemeralDataProtectionProvider());
        var dados = new CursorDados(Guid.NewGuid(), new(new(2027, 1, 1), new(2027, 1, 31), EstadoHistoricoMissao.Feita, 12, false, 20), new(2027, 1, 20), 10, 40, DateTimeOffset.UtcNow, "UTC");
        var token = codec.Codificar(dados); Assert.Equal(dados, codec.Decodificar(token));
        Assert.DoesNotContain(dados.UsuarioId.ToString(), token);
        Assert.Throws<ConsultaHistoricoInvalidaException>(() => codec.Decodificar(token[..^10] + "adulterado"));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void RecusaTamanhoForaDoLimite(int limite) => Assert.False(new ConsultarHistoricoMissoesRequestValidator().Validate(new ConsultarHistoricoMissoesRequest() { Limite = limite }).IsValid);
    [Fact]
    public void RecusaPeriodoIncompletoInvertidoOuLongoEDoisTiposDeVinculo()
    {
        var v = new ConsultarHistoricoMissoesRequestValidator();
        Assert.False(v.Validate(new ConsultarHistoricoMissoesRequest() { Inicio = new(2027, 1, 1) }).IsValid);
        Assert.False(v.Validate(new ConsultarHistoricoMissoesRequest() { Inicio = new(2027, 2, 1), Fim = new(2027, 1, 1) }).IsValid);
        Assert.False(v.Validate(new ConsultarHistoricoMissoesRequest() { Inicio = new(2000, 1, 1), Fim = new(2027, 1, 1) }).IsValid);
        Assert.False(v.Validate(new ConsultarHistoricoMissoesRequest() { CampanhaId = 1, Avulsas = true }).IsValid);
        Assert.True(v.Validate(new ConsultarHistoricoMissoesRequest()).IsValid);
    }
}
