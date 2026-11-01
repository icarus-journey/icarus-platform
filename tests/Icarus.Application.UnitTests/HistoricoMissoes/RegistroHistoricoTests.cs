using Icarus.Application.Common;
using Icarus.Application.UseCases.ConsultarHistoricoMissoes;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
namespace Icarus.Application.UnitTests.HistoricoMissoes;

public sealed class RegistroHistoricoTests
{
    [Theory]
    [InlineData(StatusOcorrencia.Pendente, 20, EstadoHistoricoMissao.Pendente)]
    [InlineData(StatusOcorrencia.Pendente, 19, EstadoHistoricoMissao.Atrasada)]
    [InlineData(StatusOcorrencia.Atrasada, 20, EstadoHistoricoMissao.Pendente)]
    [InlineData(StatusOcorrencia.Concluida, 19, EstadoHistoricoMissao.Feita)]
    [InlineData(StatusOcorrencia.ConcluidaComAtraso, 19, EstadoHistoricoMissao.FeitaComAtraso)]
    [InlineData(StatusOcorrencia.Excluida, 19, EstadoHistoricoMissao.Cancelada)]
    public void EstadoUsaExecucaoEDataLocalSemAlterarRegistro(StatusOcorrencia status, int dia, EstadoHistoricoMissao esperado)
    {
        var o = new Ocorrencia { Status = status, DataPrevista = new(2027, 1, dia), Missao = new() { Titulo = "Título" } };
        var r = RegistroHistoricoMissao.DaOcorrencia(o, new(2027, 1, 20));
        Assert.Equal(esperado, r.Estado); Assert.Equal(status, o.Status); Assert.Null(r.PontosRecebidos);
    }
    [Fact]
    public void UsaTituloPreservadoECreditoEfetivoSemConverterPontuacaoConfigurada()
    {
        var usuario = Guid.NewGuid(); var missao = new Missao { UsuarioId = usuario, Titulo = "Antes", Pontuacao = 100 };
        var o = new Ocorrencia { Missao = missao, Planejamento = PlanejamentoOcorrencia.Capturar(missao), Status = StatusOcorrencia.Concluida };
        missao.Titulo = "Depois";
        o.MovimentacoesPontos.Add(new() { UsuarioId = usuario, Tipo = TipoMovimentacaoPontos.GanhoMissao, Quantidade = 30 });
        o.MovimentacoesPontos.Add(new() { UsuarioId = Guid.NewGuid(), Tipo = TipoMovimentacaoPontos.GanhoMissao, Quantidade = 90 });
        var r = RegistroHistoricoMissao.DaOcorrencia(o, new(2027, 1, 20));
        Assert.Equal("Antes", r.Titulo); Assert.True(r.TituloPreservado); Assert.Equal(30, r.PontosRecebidos);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("{invalido")]
    public void LegadoNaoInventaVersaoRealizacaoFusoOuPontos(string? planejamento)
    {
        var o = new Ocorrencia
        {
            Missao = new() { Titulo = "Nome atual", Pontuacao = 500 },
            Planejamento = planejamento,
            Status = StatusOcorrencia.Concluida,
            FusoHorarioRealizacao = "invalido",
            ConcluidaEm = DateTimeOffset.UtcNow
        };
        var r = RegistroHistoricoMissao.DaOcorrencia(o, new(2027, 1, 20));
        Assert.False(r.TituloPreservado); Assert.Equal("Nome atual", r.Titulo); Assert.Null(r.RealizadaEm); Assert.Null(r.FusoHorarioRealizacao); Assert.Null(r.PontosRecebidos);
    }
}
