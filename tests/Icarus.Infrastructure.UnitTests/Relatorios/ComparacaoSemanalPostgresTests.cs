using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.UseCases.SolicitarRelatorioSemanal;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Icarus.Infrastructure.UnitTests.Relatorios;

public sealed class ComparacaoSemanalPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private sealed class Relogio : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Agora;
    }
    private readonly Relogio _relogio = new();
    private static readonly PeriodoAnalitico Periodo = new(new(2027, 2, 1), new(2027, 2, 7), "America/Sao_Paulo");
    private IcarusDbContext Contexto() => new(new DbContextOptionsBuilder<IcarusDbContext>()
        .UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention().Options, _relogio);
    private async Task<Guid> Pessoa()
    {
        await using var c = Contexto();
        var u = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Nome proibido no provedor",
            Email = $"{Guid.NewGuid():N}@teste.local",
            SenhaHash = "teste",
            DataNascimento = new(2000, 1, 1),
            FusoHorario = Periodo.FusoHorario
        };
        c.Usuarios.Add(u); await c.SaveChangesAsync(); return u.Id;
    }
    private static Ocorrencia Planejar(IcarusDbContext c, Guid id, DateOnly dia, DateTimeOffset? realizada = null)
    {
        var o = new Ocorrencia
        {
            Missao = new() { UsuarioId = id, Titulo = "Título pessoal proibido", Pontuacao = 30 },
            DataPrevista = dia,
            Status = realizada is null ? StatusOcorrencia.Pendente : StatusOcorrencia.Concluida,
            RealizadaEm = realizada,
            ConcluidaEm = realizada,
            FusoHorarioRealizacao = realizada is null ? null : Periodo.FusoHorario
        };
        c.Ocorrencias.Add(o); return o;
    }
    private async Task Preencher(Guid id)
    {
        await using var c = Contexto();
        var totais = new[] { (10, 8), (8, 4), (4, 2), (12, 9), (10, 5) };
        for (var i = 0; i < 5; i++)
        {
            var dia = Periodo.Inicio.AddDays(-7 * i);
            for (var j = 0; j < totais[i].Item1; j++)
                Planejar(c, id, dia, j < totais[i].Item2 ? new DateTimeOffset(dia.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero) : null);
        }
        await c.SaveChangesAsync();
    }

    [FatoPostgres]
    public async Task ComparaCincoSemanasNoMesmoFusoSemMisturarAtividadeOuTitulares()
    {
        var id = await Pessoa(); var outro = await Pessoa(); await Preencher(id); await Preencher(outro);
        await using (var c = Contexto())
        {
            var antiga = await c.Ocorrencias.Where(o => o.Missao.UsuarioId == id && o.DataPrevista == Periodo.Inicio.AddDays(-7) && o.RealizadaEm == null).FirstAsync();
            antiga.Status = StatusOcorrencia.ConcluidaComAtraso; antiga.RealizadaEm = Periodo.InicioUtc.AddDays(2); antiga.ConcluidaEm = antiga.RealizadaEm;
            await c.SaveChangesAsync();
        }
        await using var leitura = Contexto();
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularRelatorioAsync(id, Periodo, default);
        Assert.Equal(9, r.Indicadores.Concluidas); Assert.Null(r.Indicadores.Previstas); Assert.Null(r.Indicadores.TaxaConclusao);
        Assert.Equal(8, r.ComparacaoSemanal.Semana.Concluidas); Assert.Equal(80m, r.ComparacaoSemanal.Semana.TaxaConclusao);
        Assert.Equal(4, r.ComparacaoSemanal.SemanasAnteriores[0].Concluidas);
        Assert.Equal(new MediaPlanejada(8.5m, 5m, 56.2m), r.ComparacaoSemanal.MediaQuatroSemanas);
        Assert.False(r.Indicadores.HistoricoInsuficienteComparacao);
        Assert.Equal(11, r.Indicadores.QuantidadeRegistros);
        var carga = JsonSerializer.Serialize(r, ContratoRelatorioSemanal.Json);
        Assert.DoesNotContain(id.ToString(), carga); Assert.DoesNotContain("Título pessoal", carga); Assert.DoesNotContain("Nome proibido", carga);
    }

    [FatoPostgres]
    public async Task PlanejamentoSemConclusaoPermiteRelatorioDeTaxaZeroENaoInventaAtividade()
    {
        var id = await Pessoa(); await using (var c = Contexto()) { Planejar(c, id, Periodo.Inicio); await c.SaveChangesAsync(); }
        _relogio.Agora = Periodo.FimExclusivoUtc.AddDays(1);
        await using var leitura = Contexto();
        var resposta = await new SolicitarRelatorioSemanalUseCase(new RelatorioSemanalRepository(leitura),
            new IndicadoresPessoaisRepository(leitura), leitura, _relogio, NullLogger<SolicitarRelatorioSemanalUseCase>.Instance).ExecuteAsync(new(id, id), default);
        Assert.Equal("processando", resposta.Estado);
        var mensagem = JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>((await leitura.Set<OutboxRelatorio>().SingleAsync(o => leitura.Set<TrabalhoRelatorioSemanal>().Any(t => t.Id == o.TrabalhoId && t.UsuarioId == id))).Carga!, ContratoRelatorioSemanal.Json)!;
        Assert.Equal(ContratoRelatorioSemanal.Versao, mensagem.VersaoContrato);
        Assert.Equal(0, mensagem.ComparacaoSemanal!.Semana.Concluidas); Assert.Equal(0, mensagem.ComparacaoSemanal.Semana.TaxaConclusao);
        Assert.Null(mensagem.Indicadores.Concluidas); Assert.True(mensagem.Indicadores.HistoricoInsuficienteComparacao);
        Assert.All(mensagem.ComparacaoSemanal.SemanasAnteriores, s => { Assert.Equal("sem_planejamento", s.Estado); Assert.Null(s.TaxaConclusao); });
    }

    [FatoPostgres]
    public async Task LegadoOuLacunaNaoAutorizaTaxaNemMediaMesmoComMuitasConclusoes()
    {
        var id = await Pessoa(); await Preencher(id);
        await using (var c = Contexto())
        {
            (await c.Usuarios.SingleAsync(u => u.Id == id)).PlanejamentoVersionadoDesde = Periodo.InicioUtc;
            await c.SaveChangesAsync();
        }
        await using var leitura = Contexto();
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularRelatorioAsync(id, Periodo, default);
        Assert.Equal(8, r.ComparacaoSemanal.Semana.Concluidas);
        Assert.All(r.ComparacaoSemanal.SemanasAnteriores, s => { Assert.Equal("historico_parcial", s.Estado); Assert.Null(s.Previstas); Assert.Null(s.Concluidas); });
        Assert.False(r.ComparacaoSemanal.PodeCompararSemanaAnterior); Assert.Null(r.ComparacaoSemanal.MediaQuatroSemanas.TaxaConclusao);
        Assert.True(r.Indicadores.HistoricoInsuficienteComparacao);
    }

    [FatoPostgres]
    public async Task RetroativoAntesDaGeracaoUsaRealizacaoEMantemCorteLocal()
    {
        var id = await Pessoa(); await Preencher(id);
        _relogio.Agora = Periodo.FimExclusivoUtc.AddDays(2);
        await using (var c = Contexto())
        {
            var abertas = await c.Ocorrencias.Where(o => o.Missao.UsuarioId == id && o.DataPrevista == Periodo.Inicio && o.RealizadaEm == null).OrderBy(o => o.Id).ToArrayAsync();
            for (var i = 0; i < 2; i++)
            {
                abertas[i].Status = StatusOcorrencia.ConcluidaComAtraso; abertas[i].RealizadaEm = Periodo.FimExclusivoUtc.AddSeconds(i == 0 ? -1 : 0);
                abertas[i].ConcluidaEm = _relogio.Agora;
            }
            await c.SaveChangesAsync();
        }
        await using var leitura = Contexto();
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularRelatorioAsync(id, Periodo, default);
        Assert.Equal(9, r.ComparacaoSemanal.Semana.Concluidas); Assert.Equal(90m, r.ComparacaoSemanal.Semana.TaxaConclusao);
        Assert.Equal(9, r.Indicadores.Concluidas);
    }

    private sealed class Intercalar(Func<Task> alterar) : DbCommandInterceptor
    {
        private int _executado;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand comando, CommandExecutedEventData evento,
            DbDataReader resultado, CancellationToken ct = default)
        {
            if (comando.CommandText.Contains("FROM registro_planejamento_ocorrencia", StringComparison.Ordinal)
                && Interlocked.Exchange(ref _executado, 1) == 0) await alterar();
            return resultado;
        }
    }

    [FatoPostgres]
    public async Task SolicitacaoMantemAtividadeECincoSemanasNoMesmoSnapshotSobEscritaConcorrente()
    {
        var id = await Pessoa(); await Preencher(id);
        var escreveu = false;
        var interceptor = new Intercalar(async () =>
        {
            await using var c = Contexto();
            var o = await c.Ocorrencias.Where(o => o.Missao.UsuarioId == id && o.DataPrevista == Periodo.Inicio && o.RealizadaEm == null).FirstAsync();
            o.Status = StatusOcorrencia.Concluida; o.RealizadaEm = Periodo.InicioUtc.AddDays(2); o.ConcluidaEm = _relogio.Agora;
            await c.SaveChangesAsync(); escreveu = true;
        });
        _relogio.Agora = Periodo.FimExclusivoUtc.AddDays(1);
        await using var leitura = new IcarusDbContext(new DbContextOptionsBuilder<IcarusDbContext>().UseNpgsql(banco.Conexao)
            .UseSnakeCaseNamingConvention().AddInterceptors(interceptor).Options, _relogio);
        await new SolicitarRelatorioSemanalUseCase(new RelatorioSemanalRepository(leitura), new IndicadoresPessoaisRepository(leitura),
            leitura, _relogio, NullLogger<SolicitarRelatorioSemanalUseCase>.Instance).ExecuteAsync(new(id, id), default);
        Assert.True(escreveu);
        var carga = await leitura.Set<OutboxRelatorio>().Where(o => leitura.Set<TrabalhoRelatorioSemanal>().Any(t => t.Id == o.TrabalhoId && t.UsuarioId == id)).Select(o => o.Carga).SingleAsync();
        var mensagem = JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(carga!, ContratoRelatorioSemanal.Json)!;
        Assert.Equal(8, mensagem.Indicadores.Concluidas); Assert.Equal(10, mensagem.ComparacaoSemanal!.Semana.Previstas);
        Assert.Equal(8, mensagem.ComparacaoSemanal.Semana.Concluidas);
        await using var atual = Contexto();
        Assert.Equal(9, (await new IndicadoresPessoaisRepository(atual).CalcularRelatorioAsync(id, Periodo, default)).Indicadores.Concluidas);
    }
}
