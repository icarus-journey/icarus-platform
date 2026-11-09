using Icarus.Application.UseCases.ConsultarExtratoPontos;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
namespace Icarus.Application.UnitTests.ExtratoPontos;

public sealed class MovimentoExtratoTests
{
    [Fact]
    public void UsaSnapshotDoLancamentoSemSubstituirPeloPrecoOuTituloAtual()
    {
        var id = Guid.NewGuid();
        var m = new MovimentacaoPontos
        {
            Id = 1,
            UsuarioId = id,
            Tipo = TipoMovimentacaoPontos.CompraItem,
            Quantidade = -100,
            SaldoResultante = 0,
            TituloOrigem = "Nome do resgate",
            CriadoEm = new(2026, 11, 1, 2, 59, 59, TimeSpan.Zero),
            Item = new() { UsuarioId = id, Nome = "Outro nome", Valor = 999, Estado = EstadoRecompensa.Excluida }
        };
        var r = MovimentoExtratoPontos.DoMovimento(m, "America/Sao_Paulo");
        Assert.Equal(-100, r.Quantidade); Assert.Equal(0, r.SaldoApos); Assert.Equal("Nome do resgate", r.TituloOrigem);
        Assert.True(r.TituloPreservado); Assert.True(r.OrigemRemovida); Assert.Equal(new DateOnly(2026, 10, 31), r.DataLocal);
    }
    [Fact]
    public void LegadoMantemSaldoAusenteETituloAtualIdentificadoMesmoComDescricaoLivre()
    {
        var id = Guid.NewGuid();
        var m = new MovimentacaoPontos
        {
            UsuarioId = id,
            Tipo = TipoMovimentacaoPontos.GanhoMissao,
            Quantidade = 30,
            Descricao = "Não extrair título, saldo ou instruções deste texto",
            CriadoEm = DateTimeOffset.UtcNow,
            Ocorrencia = new() { Missao = new() { UsuarioId = id, Titulo = "Nome atual", Status = StatusMissao.Excluida }, Planejamento = "{invalido" }
        };
        var r = MovimentoExtratoPontos.DoMovimento(m, "UTC");
        Assert.Null(r.SaldoApos); Assert.Equal("Nome atual", r.TituloOrigem); Assert.False(r.TituloPreservado); Assert.True(r.OrigemRemovida);
        Assert.DoesNotContain("instruções", System.Text.Json.JsonSerializer.Serialize(r));
    }
    [Theory]
    [InlineData(TipoMovimentacaoPontos.GanhoMissao)]
    [InlineData(TipoMovimentacaoPontos.CompraItem)]
    public void NaoExibeTituloDeVinculoComOutroTitular(TipoMovimentacaoPontos tipo)
    {
        var m = new MovimentacaoPontos
        {
            UsuarioId = Guid.NewGuid(),
            Tipo = tipo,
            CriadoEm = DateTimeOffset.UtcNow,
            Item = new() { UsuarioId = Guid.NewGuid(), Nome = "Segredo alheio" },
            Ocorrencia = new() { Missao = new() { UsuarioId = Guid.NewGuid(), Titulo = "Segredo alheio" } }
        };
        Assert.Null(MovimentoExtratoPontos.DoMovimento(m, "UTC").TituloOrigem);
    }
}
