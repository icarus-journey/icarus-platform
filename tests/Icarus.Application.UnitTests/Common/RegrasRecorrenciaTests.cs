using Icarus.Application.Common;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.UnitTests.Common;

public sealed class RegrasRecorrenciaTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 30);
    private static Missao Nova() => new()
    {
        Id = 1,
        Pontuacao = 30,
        Recorrencia = new() { Tipo = "DIARIA", DataInicio = Hoje.AddDays(-30), Regra = "{}" }
    };

    [Fact]
    public void CanceladaHojeNaoMaterializaOutraNemVoltaASelecionarFuturo()
    {
        var m = Nova(); m.Ocorrencias = [new() { DataPrevista = Hoje, Status = StatusOcorrencia.Excluida }, new() { DataPrevista = Hoje.AddDays(1) }];
        Assert.Null(RegrasRecorrencia.Selecionar(m, Hoje));
        Assert.Equal(2, m.Ocorrencias.Count);
    }

    [Fact]
    public void LegadoSemOcorrenciasRecuperaSomenteHojePrevistoNaRegraVigente()
    {
        var m = Nova();
        Assert.Equal(Hoje, RegrasRecorrencia.Selecionar(m, Hoje)!.DataPrevista);
        m.Recorrencia!.Tipo = "SEMANAL";
        m.Recorrencia.Regra = GeradorDeDatasRecorrencia.ConstruirRegra(TipoRecorrencia.Semanal, [Hoje.AddDays(1).DayOfWeek], 1);
        Assert.Null(RegrasRecorrencia.Selecionar(m, Hoje));
        m.Recorrencia.Tipo = "DIARIA"; m.Recorrencia.AlteracoesAPartirDe = Hoje.AddDays(1);
        Assert.Null(RegrasRecorrencia.Selecionar(m, Hoje));
        Assert.Empty(m.Ocorrencias);
    }

    [Fact]
    public void UnicaLegadaSoMaterializaDataExplicitaDentroDosLimites()
    {
        var m = Nova(); m.Recorrencia = null; m.DataLimite = Hoje.AddDays(1);
        Assert.Null(RegrasRecorrencia.Selecionar(m, Hoje));
        m.DataLimite = Hoje.AddDays(-1);
        Assert.Equal(m.DataLimite, RegrasRecorrencia.Selecionar(m, Hoje)!.DataPrevista);
        m.Campanha = new() { PrazoInicio = Hoje, PrazoFim = Hoje.AddDays(7) };
        Assert.Null(RegrasRecorrencia.Selecionar(m, Hoje));
    }

    [Fact]
    public void ConcluidaHojeTemPrioridadeSobreOutroAtrasoSemRecreditar()
    {
        var m = Nova(); var feita = new Ocorrencia { Id = 2, DataPrevista = Hoje, Status = StatusOcorrencia.Concluida, ConcluidaEm = DateTimeOffset.UtcNow };
        m.Ocorrencias = [new() { DataPrevista = Hoje.AddDays(-1) }, feita];
        Assert.Same(feita, RegrasRecorrencia.Selecionar(m, Hoje));
    }

    [Fact]
    public void AtrasoAnteriorAoInicioDaCampanhaNaoEhElegivel()
    {
        var m = Nova(); m.Campanha = new() { PrazoInicio = Hoje, PrazoFim = Hoje.AddDays(5) };
        m.Ocorrencias = [new() { DataPrevista = Hoje.AddDays(-1) }, new() { DataPrevista = Hoje, Status = StatusOcorrencia.Excluida }];
        Assert.Null(RegrasRecorrencia.Selecionar(m, Hoje));
    }

    [Fact]
    public void JanelaRespeitaVigenciaDaEdicaoEIntersecaoDosPrazos()
    {
        var m = Nova(); m.Recorrencia!.DataFim = Hoje.AddDays(10); m.Recorrencia.AlteracoesAPartirDe = Hoje.AddDays(2);
        m.Campanha = new() { PrazoInicio = Hoje.AddDays(1), PrazoFim = Hoje.AddDays(5) };
        Assert.Equal((Hoje.AddDays(2), Hoje.AddDays(5)), RegrasRecorrencia.Janela(m, Hoje));
    }

    [Fact]
    public void FimEhInclusivoEEncerramentoPreservaAtrasosEConclusoes()
    {
        var m = Nova(); m.Recorrencia!.DataFim = Hoje;
        m.Ocorrencias = [new() { DataPrevista = Hoje }, new() { DataPrevista = Hoje.AddDays(1) },
            new() { DataPrevista = Hoje.AddDays(2), Status = StatusOcorrencia.Concluida, ConcluidaEm = DateTimeOffset.UtcNow }];
        Assert.False(RegrasRecorrencia.EncerrarSeNecessario(m, Hoje));
        Assert.True(RegrasRecorrencia.EncerrarSeNecessario(m, Hoje.AddDays(1)));
        Assert.Equal(StatusOcorrencia.Pendente, m.Ocorrencias.First().Status);
        Assert.Equal(StatusOcorrencia.Excluida, m.Ocorrencias.ElementAt(1).Status);
        Assert.Equal(StatusOcorrencia.Concluida, m.Ocorrencias.Last().Status);
        Assert.False(RegrasRecorrencia.EncerrarSeNecessario(m, Hoje.AddDays(2)));
    }
}
