using System.Data.Common;
using Icarus.Application.Common.Analise;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Analise;

public sealed class LeiturasAnaliticasPostgresTests(BancoLoja banco, ITestOutputHelper saida) : IClassFixture<BancoLoja>
{
    private static readonly DateOnly Inicio = new(2027, 6, 1);
    private static readonly PeriodoAnalitico Semana = new(new(2027, 12, 1), new(2027, 12, 7), "UTC");

    private sealed class Relogio : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class ContadorLeituras : DbCommandInterceptor
    {
        public int Consultas { get; private set; }
        public int Leituras { get; private set; }

        public override InterceptionResult DataReaderDisposing(DbCommand command,
            DataReaderDisposingEventData eventData, InterceptionResult result)
        {
            Consultas++;
            Leituras += eventData.ReadCount;
            return result;
        }
    }

    private IcarusDbContext Contexto(ContadorLeituras? contador = null)
    {
        var opcoes = new DbContextOptionsBuilder<IcarusDbContext>().UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention();
        if (contador is not null) opcoes.AddInterceptors(contador);
        return new(opcoes.Options, new Relogio());
    }

    private async Task<Guid> CriarHistorico()
    {
        await using var contexto = Contexto();
        var pessoa = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Pessoa de teste",
            Email = $"{Guid.NewGuid():N}@teste.local",
            SenhaHash = "hash-de-teste",
            DataNascimento = new(1995, 1, 1),
            FusoHorario = "UTC",
            PlanejamentoVersionadoDesde = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero),
            ProtecaoAtivadaEm = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero),
            FusoInicioProtecao = "UTC"
        };
        contexto.Usuarios.Add(pessoa);
        for (var i = 0; i < 3; i++)
        {
            var missao = new Missao
            {
                UsuarioId = pessoa.Id,
                Titulo = "Histórico de teste",
                Pontuacao = 30,
                DataLimite = Inicio,
                Horario = new(2027, 6, 1, 9, 0, 0, TimeSpan.Zero),
                Status = StatusMissao.Ativa,
                Recorrencia = new() { Tipo = "Diaria", Regra = "{}", DataInicio = Inicio, DataFim = Semana.Fim }
            };
            contexto.Missoes.Add(missao);
            for (var data = Inicio; data <= Semana.Fim; data = data.AddDays(1))
            {
                var realizada = new DateTimeOffset(data.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
                contexto.Ocorrencias.Add(new()
                {
                    Missao = missao,
                    DataPrevista = data,
                    Status = StatusOcorrencia.Concluida,
                    RealizadaEm = realizada,
                    ConcluidaEm = realizada,
                    FusoHorarioRealizacao = "UTC"
                });
            }
        }
        await contexto.SaveChangesAsync();
        return pessoa.Id;
    }

    [FatoPostgres]
    public async Task RelatorioDeCincoSemanasLimitaConsultasELeiturasComHistoricoLongo()
    {
        var id = await CriarHistorico();
        var contador = new ContadorLeituras();
        await using var contexto = Contexto(contador);
        var resposta = await new IndicadoresPessoaisRepository(contexto).CalcularRelatorioAsync(id, Semana, default);
        saida.WriteLine($"Relatório: {contador.Consultas} consultas; {contador.Leituras} operações de leitura do cursor.");
        Assert.Equal(21, resposta.ComparacaoSemanal.Semana.Previstas);
        Assert.Equal(21, resposta.ComparacaoSemanal.Semana.Concluidas);
        Assert.Equal(100, resposta.ComparacaoSemanal.Semana.TaxaConclusao);
        Assert.False(resposta.Indicadores.HistoricoInsuficienteComparacao);
        Assert.InRange(contador.Consultas, 1, 10);
        Assert.InRange(contador.Leituras, 1, 400);
    }

    [FatoPostgres]
    public async Task ProgressoSemanalNaoTransfereTodoOHistoricoDoUsuario()
    {
        var id = await CriarHistorico();
        var contador = new ContadorLeituras();
        await using var contexto = Contexto(contador);
        var resposta = await new IndicadoresPessoaisRepository(contexto).CalcularProgressoAsync(id, Semana, default);
        saida.WriteLine($"Progresso: {contador.Consultas} consultas; {contador.Leituras} operações de leitura do cursor.");
        Assert.Equal(21, resposta.Adesao!.PrevistasRegistradas);
        Assert.Equal(21, resposta.Adesao.ConcluidasDoPlanejamento);
        Assert.Equal(21, resposta.Indicadores.Concluidas);
        Assert.InRange(contador.Consultas, 1, 14);
        Assert.InRange(contador.Leituras, 1, 120);
    }

    [FatoPostgres]
    public async Task ConsultaDaLojaNaoRepeteAConciliacaoDepoisDeLer()
    {
        var id = await CriarHistorico();
        var contador = new ContadorLeituras();
        await using var contexto = Contexto(contador);
        var repositorio = new LojaRepository(contexto, new ProtecaoSequenciaRepository(contexto, new Relogio()));
        var resposta = await new ConsultarLojaUseCase(repositorio, new LojaOptions()).ExecuteAsync(new(id, id), default);
        saida.WriteLine($"Loja: {contador.Consultas} consultas; {contador.Leituras} operações de leitura do cursor.");
        Assert.Equal(0, resposta.SaldoPontos);
        Assert.Equal(0, resposta.Protecao.Quantidade);
        Assert.Empty(resposta.Recompensas);
        Assert.InRange(contador.Consultas, 1, 8);
    }
}
