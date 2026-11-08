using Icarus.Application.Common;
using Icarus.Application.Common.Analise;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Icarus.Infrastructure.UnitTests.Analise;

public sealed class AdesaoPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private sealed class Relogio : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = Instante(1);
        public override DateTimeOffset GetUtcNow() => Agora;
    }
    private static DateTimeOffset Instante(int dia, int hora = 12) => new(2027, 1, dia, hora, 0, 0, TimeSpan.Zero);
    private static readonly PeriodoAnalitico Semana = new(new(2027, 1, 4), new(2027, 1, 10), "UTC");
    private IcarusDbContext Contexto(Relogio relogio) => new(new DbContextOptionsBuilder<IcarusDbContext>()
        .UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention().Options, relogio);
    private async Task<Guid> Pessoa(Relogio relogio)
    {
        await using var c = Contexto(relogio);
        var u = new Usuario { Id = Guid.NewGuid(), Nome = "Somente teste", Email = $"{Guid.NewGuid():N}@teste.local", SenhaHash = "teste", DataNascimento = new(2000, 1, 1), FusoHorario = "UTC" };
        c.Usuarios.Add(u); await c.SaveChangesAsync(); return u.Id;
    }
    private static Ocorrencia Planejar(IcarusDbContext c, Guid id, int dia)
    {
        var o = new Ocorrencia { Missao = new() { UsuarioId = id, Titulo = "Texto não sai nos indicadores", Pontuacao = 30 }, DataPrevista = new(2027, 1, dia) };
        c.Ocorrencias.Add(o); return o;
    }
    private static void Concluir(Ocorrencia o, int dia)
    {
        o.Status = StatusOcorrencia.Concluida; o.RealizadaEm = Instante(dia); o.ConcluidaEm = Instante(dia); o.FusoHorarioRealizacao = "UTC";
    }

    [FatoPostgres]
    public async Task CalculaPlanejamentoCompletoSemMisturarAtividadeEAdesaoOuOutrasContas()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); var outro = await Pessoa(relogio);
        await using (var c = Contexto(relogio))
        {
            for (var i = 0; i < 10; i++) { var o = Planejar(c, id, 6); if (i < 8) Concluir(o, 6); }
            Concluir(Planejar(c, id, 3), 6); Concluir(Planejar(c, outro, 6), 6);
            await c.SaveChangesAsync();
        }
        await using var leitura = Contexto(relogio);
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal(9, r.Indicadores.Concluidas); Assert.Equal(9, r.Evolucao!.Pontos.Sum(p => p.Concluidas));
        Assert.Equal(10, r.Indicadores.Previstas); Assert.Equal(80, r.Indicadores.TaxaConclusao);
        Assert.Equal(8, r.Adesao!.ConcluidasDoPlanejamento); Assert.Equal(2, r.Adesao.Atrasadas);
        Assert.Equal(11, r.Indicadores.QuantidadeRegistros);
        var consumidor = await new IndicadoresPessoaisRepository(leitura).CalcularAsync(id, Semana, default);
        Assert.Null(consumidor.Previstas); Assert.Null(consumidor.TaxaConclusao); Assert.Equal(9, consumidor.Concluidas);
        Assert.DoesNotContain("Texto não sai", System.Text.Json.JsonSerializer.Serialize(r));
    }

    [FatoPostgres]
    public async Task RealizacaoPosteriorApareceNaSemanaSeguinteERetroativoValidoRecalcula()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); long primeiro; long segundo;
        await using (var c = Contexto(relogio))
        {
            var a = Planejar(c, id, 6); var b = Planejar(c, id, 7); await c.SaveChangesAsync(); primeiro = a.Id; segundo = b.Id;
        }
        relogio.Agora = Instante(12);
        await using (var c = Contexto(relogio)) { Concluir((await c.Ocorrencias.FindAsync(primeiro))!, 12); Concluir((await c.Ocorrencias.FindAsync(segundo))!, 10); await c.SaveChangesAsync(); }
        await using var leitura = Contexto(relogio);
        var repo = new IndicadoresPessoaisRepository(leitura);
        var antiga = await repo.CalcularProgressoAsync(id, Semana, default);
        Assert.Equal(50, antiga.Adesao!.TaxaConclusao); Assert.Equal(1, antiga.Indicadores.Concluidas);
        var seguinte = await repo.CalcularProgressoAsync(id, new(new(2027, 1, 11), new(2027, 1, 17), "UTC"), default);
        Assert.Equal(1, seguinte.Indicadores.Concluidas); Assert.Equal("sem_planejamento", seguinte.Adesao!.Estado);
        Assert.Equal(2, await leitura.Set<RegistroPlanejamentoOcorrencia>().CountAsync(v => v.Ocorrencia.Missao.UsuarioId == id));
    }

    [FatoPostgres]
    public async Task CancelarOuReagendarDepoisNaoApagaPlanejamentoDaSemanaAnterior()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); long a; long b;
        await using (var c = Contexto(relogio)) { var x = Planejar(c, id, 6); var y = Planejar(c, id, 7); await c.SaveChangesAsync(); a = x.Id; b = y.Id; }
        relogio.Agora = Instante(12);
        await using (var c = Contexto(relogio)) { (await c.Ocorrencias.FindAsync(a))!.Status = StatusOcorrencia.Excluida; (await c.Ocorrencias.FindAsync(b))!.DataPrevista = new(2027, 1, 15); await c.SaveChangesAsync(); }
        await using var leitura = Contexto(relogio);
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal(2, r.Adesao!.PrevistasRegistradas); Assert.Equal(0, r.Adesao.TaxaConclusao);
        Assert.Equal(2, r.Adesao.Atrasadas);
        Assert.Equal(4, await leitura.Set<RegistroPlanejamentoOcorrencia>().CountAsync(v => v.Ocorrencia.Missao.UsuarioId == id));
    }

    [FatoPostgres]
    public async Task CancelarAntesDoFechamentoERetirarOPaiPreservaSoAOrigemElegivel()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); long a; long b;
        await using (var c = Contexto(relogio)) { var x = Planejar(c, id, 6); var y = Planejar(c, id, 7); Concluir(y, 7); await c.SaveChangesAsync(); a = x.Id; b = y.MissaoId; }
        relogio.Agora = Instante(9);
        await using (var c = Contexto(relogio)) { (await c.Ocorrencias.FindAsync(a))!.Status = StatusOcorrencia.Excluida; (await c.Missoes.FindAsync(b))!.Status = StatusMissao.Excluida; await c.SaveChangesAsync(); }
        await using var leitura = Contexto(relogio);
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal(1, r.Adesao!.PrevistasRegistradas); Assert.Equal(100, r.Adesao.TaxaConclusao);
        Assert.Equal(1, r.Indicadores.Concluidas);
    }

    [FatoPostgres]
    public async Task LegadoPreservaObservadasSemAtribuirCompletudeOuPorcentagem()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); relogio.Agora = Instante(15);
        await using (var c = Contexto(relogio)) { (await c.Usuarios.FindAsync(id))!.PlanejamentoVersionadoDesde = Instante(15); Concluir(Planejar(c, id, 6), 6); Planejar(c, id, 7); await c.SaveChangesAsync(); }
        await using var leitura = Contexto(relogio);
        var antes = await leitura.Set<RegistroPlanejamentoOcorrencia>().CountAsync();
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal("historico_parcial", r.Adesao!.Estado); Assert.Equal(2, r.Adesao.PrevistasRegistradas);
        Assert.Null(r.Indicadores.TaxaConclusao); Assert.Null(r.Indicadores.Previstas);
        Assert.Equal(1, r.Indicadores.Concluidas);
        Assert.Equal(antes, await leitura.Set<RegistroPlanejamentoOcorrencia>().CountAsync());
    }

    [FatoPostgres]
    public async Task FalhaDaJanelaRecorrenteNaoViraUmaTaxaArtificial()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio);
        await using (var c = Contexto(relogio))
        {
            var m = new Missao { UsuarioId = id, Titulo = "Recorrente", Recorrencia = new() { Tipo = "DIARIA", Regra = "{}", DataInicio = new(2027, 1, 4), DataFim = new(2027, 1, 10) } };
            for (var dia = 4; dia <= 10; dia++) if (dia != 7) m.Ocorrencias.Add(new() { DataPrevista = new(2027, 1, dia) });
            c.Missoes.Add(m); await c.SaveChangesAsync();
        }
        await using var leitura = Contexto(relogio);
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal("janela_incompleta", r.Adesao!.Motivo); Assert.Null(r.Adesao.TaxaConclusao);
        Assert.Equal(6, await leitura.Ocorrencias.CountAsync(o => o.Missao.UsuarioId == id));
    }

    [FatoPostgres]
    public async Task FalhaDeGravacaoRevertePlanejamentoEHistoricoEHistoricoNaoPodeSerEditado()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); long oid;
        await using (var c = Contexto(relogio)) { var o = Planejar(c, id, 6); await c.SaveChangesAsync(); oid = o.Id; }
        relogio.Agora = Instante(12);
        await using (var c = Contexto(relogio))
        {
            var o = await c.Ocorrencias.Include(o => o.Missao).SingleAsync(o => o.Id == oid);
            o.DataPrevista = new(2027, 1, 15); o.Missao.Titulo = new string('x', 201);
            await Assert.ThrowsAsync<DbUpdateException>(() => c.SaveChangesAsync());
        }
        await using var leitura = Contexto(relogio);
        Assert.Equal(new DateOnly(2027, 1, 6), (await leitura.Ocorrencias.FindAsync(oid))!.DataPrevista);
        var versao = await leitura.Set<RegistroPlanejamentoOcorrencia>().SingleAsync(v => v.OcorrenciaId == oid);
        versao.DataPrevista = new(2027, 1, 15);
        await Assert.ThrowsAsync<InvalidOperationException>(() => leitura.SaveChangesAsync());
    }

    [FatoPostgres]
    public async Task RegrasAnterioresDaRecorrenciaSaoUsadasSemRecriarDatas()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); long mid;
        await using (var c = Contexto(relogio))
        {
            var m = new Missao { UsuarioId = id, Titulo = "Repetir", Recorrencia = new() { Tipo = "DIARIA", Regra = "{}", DataInicio = new(2027, 1, 4), DataFim = new(2027, 1, 10) } };
            for (var dia = 4; dia <= 10; dia++) m.Ocorrencias.Add(new() { DataPrevista = new(2027, 1, dia) });
            c.Missoes.Add(m); await c.SaveChangesAsync(); mid = m.Id;
        }
        relogio.Agora = Instante(6);
        await using (var c = Contexto(relogio))
        {
            var m = await c.Missoes.Include(m => m.Recorrencia).Include(m => m.Ocorrencias).SingleAsync(m => m.Id == mid);
            PlanejamentoOcorrencia.RegistrarVersaoAnterior(m, new(2027, 1, 6));
            m.Recorrencia!.Tipo = "SEMANAL";
            m.Recorrencia.Regra = GeradorDeDatasRecorrencia.ConstruirRegra(TipoRecorrencia.Semanal, [DayOfWeek.Saturday], 1);
            foreach (var o in m.Ocorrencias.Where(o => o.DataPrevista.Day > 6 && o.DataPrevista.Day != 9)) o.Status = StatusOcorrencia.Excluida;
            await c.SaveChangesAsync();
        }
        await using var leitura = Contexto(relogio);
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal("disponivel", r.Adesao!.Estado); Assert.Equal(4, r.Adesao.PrevistasRegistradas);
        Assert.Equal(7, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == mid));
    }

    [FatoPostgres]
    public async Task LimitesUtcDoPeriodoMantemOFechamentoNoFusoEscolhido()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio);
        await using (var c = Contexto(relogio))
        {
            var a = Planejar(c, id, 10); var b = Planejar(c, id, 10);
            Concluir(a, 10); Concluir(b, 11);
            a.RealizadaEm = Instante(11, 2).AddMinutes(59); // Ainda domingo em São Paulo.
            b.RealizadaEm = Instante(11, 3); // Segunda-feira.
            await c.SaveChangesAsync();
        }
        await using var leitura = Contexto(relogio);
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id,
            Semana with { FusoHorario = "America/Sao_Paulo" }, default);
        Assert.Equal(50, r.Adesao!.TaxaConclusao); Assert.Equal(1, r.Indicadores.Concluidas);
    }

    [FatoPostgres]
    public async Task LeituraConcorrenteNaoMisturaAtividadeAntigaComAdesaoNova()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); long oid;
        await using (var c = Contexto(relogio)) { var o = Planejar(c, id, 6); await c.SaveChangesAsync(); oid = o.Id; }
        async Task Alterar()
        {
            await using var escrita = Contexto(relogio);
            Concluir((await escrita.Ocorrencias.FindAsync(oid))!, 6);
            await escrita.SaveChangesAsync();
        }
        var opcoes = new DbContextOptionsBuilder<IcarusDbContext>().UseNpgsql(banco.Conexao)
            .UseSnakeCaseNamingConvention().AddInterceptors(new ConclusaoDuranteLeitura(Alterar)).Options;
        await using var leitura = new IcarusDbContext(opcoes, relogio);
        var repo = new IndicadoresPessoaisRepository(leitura);
        var primeira = await repo.CalcularProgressoAsync(id, Semana, default);
        Assert.Null(primeira.Indicadores.Concluidas); Assert.Equal(0, primeira.Adesao!.ConcluidasDoPlanejamento);
        var segunda = await repo.CalcularProgressoAsync(id, Semana, default);
        Assert.Equal(1, segunda.Indicadores.Concluidas); Assert.Equal(100, segunda.Adesao!.TaxaConclusao);
    }

    [FatoPostgres]
    public async Task ReagendamentoParaForaDaJanelaAntesDoCorteRemoveOPlanejamentoOriginal()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio); long ocorrencia;
        await using (var c = Contexto(relogio))
        {
            var o = Planejar(c, id, 6); await c.SaveChangesAsync(); ocorrencia = o.Id;
        }
        relogio.Agora = Instante(8);
        await using (var c = Contexto(relogio))
        {
            (await c.Ocorrencias.FindAsync(ocorrencia))!.DataPrevista = new(2027, 1, 20);
            await c.SaveChangesAsync();
        }
        relogio.Agora = Instante(21);
        await using var leitura = Contexto(relogio);
        var resultado = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal("sem_planejamento", resultado.Adesao!.Estado);
        Assert.Equal(0, resultado.Adesao.PrevistasRegistradas);
        Assert.Null(resultado.Adesao.TaxaConclusao);
    }

    [FatoPostgres]
    public async Task LegadoAnteriorAJanelaAindaImpedeAfirmarCompletude()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio);
        await using (var c = Contexto(relogio))
        {
            var antiga = Planejar(c, id, 2); Concluir(antiga, 2);
            Planejar(c, id, 6); await c.SaveChangesAsync();
            await c.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM registro_planejamento_ocorrencia WHERE ocorrencia_id={antiga.Id}");
        }
        await using var leitura = Contexto(relogio);
        var resultado = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal("historico_parcial", resultado.Adesao!.Estado);
        Assert.Equal("planejamento_legado", resultado.Adesao.Motivo);
        Assert.Equal(1, resultado.Adesao.PrevistasRegistradas);
        Assert.Null(resultado.Adesao.TaxaConclusao);
        Assert.Null(resultado.Indicadores.Concluidas);
    }

    private sealed class ConclusaoDuranteLeitura(Func<Task> alterar) : DbCommandInterceptor
    {
        private bool _executada;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!_executada && command.CommandText.Contains("FROM ocorrencia AS o", StringComparison.Ordinal)
                && command.CommandText.Contains("realizada_em", StringComparison.Ordinal))
            {
                _executada = true;
                await alterar();
            }
            return result;
        }
    }

    [FatoPostgres]
    public async Task MissaoDatadaSemOcorrenciaNaoFicaInvisivelNoDenominador()
    {
        var relogio = new Relogio(); var id = await Pessoa(relogio);
        await using (var c = Contexto(relogio))
        {
            Concluir(Planejar(c, id, 6), 6);
            c.Missoes.Add(new() { UsuarioId = id, Titulo = "Legado sem execução", DataLimite = new(2027, 1, 7) });
            await c.SaveChangesAsync();
        }
        await using var leitura = Contexto(relogio);
        var r = await new IndicadoresPessoaisRepository(leitura).CalcularProgressoAsync(id, Semana, default);
        Assert.Equal("historico_parcial", r.Adesao!.Estado); Assert.Null(r.Adesao.TaxaConclusao);
        Assert.Equal(1, await leitura.Ocorrencias.CountAsync(o => o.Missao.UsuarioId == id));
    }
}
