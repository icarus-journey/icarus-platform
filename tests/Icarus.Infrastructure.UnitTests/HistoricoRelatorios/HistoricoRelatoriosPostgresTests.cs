using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.ConsultarHistoricoRelatorios;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
namespace Icarus.Infrastructure.UnitTests.HistoricoRelatorios;

public sealed class HistoricoRelatoriosPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Agora = new(2027, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private IcarusDbContext Contexto() => new(new DbContextOptionsBuilder<IcarusDbContext>().UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention().Options, new Relogio());
    private static string Conteudo()
    {
        var i = new IndicadoresPessoais(null, 2, null, null, null, 0, null, 60, null, 0, 0, [], null, null, null, 2, 2, true, []);
        return JsonSerializer.Serialize(new SnapshotRelatorioSemanal(new("Semana preservada", "Duas atividades registradas.", "Escolha um próximo passo possível.", "Qual ajuste cabe na sua rotina?"), i, new("provedor-original", "modelo-original")), ContratoRelatorioSemanal.Json);
    }
    private async Task<Relatorio> Salvar(Guid titular, DateOnly inicio, bool trabalho = true, string? conteudo = null)
    {
        await using var c = Contexto();
        var r = new Relatorio { UsuarioId = titular, Tipo = TipoRelatorio.Semanal, PeriodoInicio = inicio, PeriodoFim = inicio.AddDays(6), Conteudo = conteudo ?? Conteudo() };
        c.Relatorios.Add(r);
        if (trabalho) c.Set<TrabalhoRelatorioSemanal>().Add(new() { Id = Guid.NewGuid(), UsuarioId = titular, PeriodoInicio = r.PeriodoInicio, PeriodoFim = r.PeriodoFim, FusoHorario = "America/Sao_Paulo", Estado = "concluido", TentativaId = Guid.NewGuid(), ExpiraEm = Agora, Indicadores = "{}" });
        await c.SaveChangesAsync(); await c.Entry(r).ReloadAsync(); return r;
    }
    [FatoPostgres]
    public async Task PreservaDadosOriginaisAposMudarPerfilENaoEscreveAoLer()
    {
        var u = await banco.CriarUsuarioAsync(100); var outro = await banco.CriarUsuarioAsync(999);
        var r = await Salvar(u, new(2027, 2, 15)); var alheio = await Salvar(outro, new(2027, 2, 15));
        await using var c = Contexto();
        (await c.Usuarios.SingleAsync(x => x.Id == u)).FusoHorario = "Asia/Tokyo"; await c.SaveChangesAsync();
        var repo = new HistoricoRelatoriosRepository(c);
        var p = await repo.ListarAsync(u, 20, Agora, null, default);
        Assert.Equal(r.Id, Assert.Single(p.Relatorios).Id); Assert.Equal("America/Sao_Paulo", p.Relatorios[0].Periodo.FusoHorario);
        var d = await repo.ConsultarAsync(u, r.Id, default); Assert.NotNull(d);
        Assert.Equal("modelo-original", d.Relatorio.Geracao.Modelo); Assert.Equal(2, d.Relatorio.Indicadores.Concluidas);
        Assert.Equal("nao_registradas", d.DisponibilidadeReferencias); Assert.Null(d.Resumo.Planejamento);
        Assert.Null(await repo.ConsultarAsync(u, alheio.Id, default));
        Assert.Equal(r.Conteudo, (await c.Relatorios.AsNoTracking().SingleAsync(x => x.Id == r.Id)).Conteudo);
        Assert.Equal(100, (await c.Usuarios.AsNoTracking().SingleAsync(x => x.Id == u)).SaldoPontos);
        Assert.Equal(2, await c.Set<TrabalhoRelatorioSemanal>().CountAsync(t => t.UsuarioId == u || t.UsuarioId == outro));
        Assert.Empty(await c.Set<OutboxRelatorio>().ToListAsync()); Assert.Empty(await c.Set<InboxRelatorio>().ToListAsync());
    }
    [FatoPostgres]
    public async Task LegadoSemTrabalhoPreservaAusenciaDeFusoEDeComparacao()
    {
        var u = await banco.CriarUsuarioAsync(0); var json = System.Text.Json.Nodes.JsonNode.Parse(Conteudo())!; json.AsObject().Remove("versaoIndicadores");
        var r = await Salvar(u, new(2026, 12, 7), false, json.ToJsonString());
        await using var c = Contexto(); var repo = new HistoricoRelatoriosRepository(c);
        var d = await repo.ConsultarAsync(u, r.Id, default); Assert.NotNull(d);
        Assert.Null(d.Resumo.Periodo.FusoHorario); Assert.Null(d.Relatorio.ComparacaoSemanal); Assert.Equal("1.0.0", d.Relatorio.VersaoIndicadores);
        Assert.Equal(r.Conteudo, (await c.Relatorios.AsNoTracking().SingleAsync(x => x.Id == r.Id)).Conteudo);
    }
    [FatoPostgres]
    public async Task PaginacaoOrdenaPorSemanaDesempataPorIdEAncoraNovasInsercoes()
    {
        var u = await banco.CriarUsuarioAsync(0); var datas = new[] { new DateOnly(2027, 2, 1), new(2027, 1, 25), new(2027, 2, 15), new(2027, 2, 8) };
        var salvos = new List<Relatorio>(); foreach (var dia in datas) salvos.Add(await Salvar(u, dia));
        await using var c = Contexto(); var repo = new HistoricoRelatoriosRepository(c);
        var a = await repo.ListarAsync(u, 2, Agora, null, default); Assert.True(a.TemMais);
        Assert.Equal(new[] { new DateOnly(2027, 2, 15), new(2027, 2, 8) }, a.Relatorios.Select(x => x.Periodo.Inicio));
        await Salvar(u, new(2027, 1, 18));
        var ultimo = a.Relatorios[^1]; var cursor = new CursorHistoricoRelatorios(u, 2, ultimo.Periodo.Inicio, ultimo.Id, a.LimiteId, Agora);
        var b = await repo.ListarAsync(u, 2, Agora, cursor, default); Assert.False(b.TemMais);
        Assert.Equal(new[] { new DateOnly(2027, 2, 1), new(2027, 1, 25) }, b.Relatorios.Select(x => x.Periodo.Inicio));
        Assert.Empty(a.Relatorios.Select(x => x.Id).Intersect(b.Relatorios.Select(x => x.Id)));
        Assert.Equal(5, (await repo.ListarAsync(u, 20, Agora, null, default)).Relatorios.Count);
    }
    [FatoPostgres]
    public async Task RegistroIncompativelFicaVisivelMasDetalheFalhaSemExporConteudoInterno()
    {
        var u = await banco.CriarUsuarioAsync(0); var r = await Salvar(u, new(2027, 1, 4), false, "{\"diagnosticoInterno\":\"nao-expor\"}");
        await using var c = Contexto(); var repo = new HistoricoRelatoriosRepository(c);
        var resumo = Assert.Single((await repo.ListarAsync(u, 20, Agora, null, default)).Relatorios);
        Assert.Equal("indisponivel", resumo.Estado); Assert.Null(resumo.Titulo); Assert.Null(resumo.Concluidas);
        await Assert.ThrowsAsync<RelatorioSalvoIndisponivelException>(() => repo.ConsultarAsync(u, r.Id, default));
        Assert.Null(await repo.ConsultarAsync(u, long.MaxValue, default));
    }
    [FatoPostgres]
    public async Task ComparacaoUsaFusoENumerosDoSnapshotMesmoSemTrabalho()
    {
        var u = await banco.CriarUsuarioAsync(0); var inicio = new DateOnly(2027, 2, 8);
        var periodo = new PeriodoAnalitico(inicio, inicio.AddDays(6), "America/Sao_Paulo");
        var semana = new SemanaPlanejada(periodo, "disponivel", 2, 1, 50m);
        var anteriores = Enumerable.Range(1, 4).Select(i => new SemanaPlanejada(new(inicio.AddDays(-7 * i), inicio.AddDays(6 - 7 * i), periodo.FusoHorario), "disponivel", 2, 1, 50m)).ToArray();
        var snap = JsonSerializer.Deserialize<SnapshotRelatorioSemanal>(Conteudo(), ContratoRelatorioSemanal.Json)! with { ComparacaoSemanal = CalculadorComparacaoSemanal.Calcular(semana, anteriores), VersaoIndicadores = "1.1.0" };
        var r = await Salvar(u, inicio, false, JsonSerializer.Serialize(snap, ContratoRelatorioSemanal.Json));
        await using var c = Contexto(); var d = await new HistoricoRelatoriosRepository(c).ConsultarAsync(u, r.Id, default);
        Assert.NotNull(d); Assert.Equal("America/Sao_Paulo", d.Resumo.Periodo.FusoHorario);
        Assert.Equal(1, d.Resumo.Planejamento!.Concluidas); Assert.Equal(50m, d.Resumo.Planejamento.TaxaConclusao);
        Assert.Equal(2, d.Resumo.Concluidas); Assert.Equal(snap.ComparacaoSemanal!.Semana, d.Relatorio.ComparacaoSemanal!.Semana);
    }
    [FatoPostgres]
    public async Task DesempataMesmaDataSemDescartarLinhaIncompativel()
    {
        var u = await banco.CriarUsuarioAsync(0); var inicio = new DateOnly(2027, 1, 4); var normal = await Salvar(u, inicio, false);
        await using var c = Contexto(); var incompleto = new Relatorio { UsuarioId = u, Tipo = TipoRelatorio.Semanal, PeriodoInicio = inicio, PeriodoFim = inicio.AddDays(4), Conteudo = Conteudo() };
        c.Relatorios.Add(incompleto); await c.SaveChangesAsync();
        var repo = new HistoricoRelatoriosRepository(c); var p = await repo.ListarAsync(u, 1, Agora, null, default);
        Assert.Equal(incompleto.Id, Assert.Single(p.Relatorios).Id); Assert.Equal("indisponivel", p.Relatorios[0].Estado);
        var cursor = new CursorHistoricoRelatorios(u, 1, inicio, incompleto.Id, p.LimiteId, Agora);
        Assert.Equal(normal.Id, Assert.Single((await repo.ListarAsync(u, 1, Agora, cursor, default)).Relatorios).Id);
    }

    [FatoPostgres]
    public async Task ContagemAusenteNoFormatoAntigoNaoViraZero()
    {
        var u = await banco.CriarUsuarioAsync(0);
        var json = System.Text.Json.Nodes.JsonNode.Parse(Conteudo())!;
        json["indicadores"]!.AsObject().Remove("diasComConclusao");
        var r = await Salvar(u, new(2027, 1, 11), false, json.ToJsonString());
        await using var c = Contexto(); var repo = new HistoricoRelatoriosRepository(c);
        var resumo = Assert.Single((await repo.ListarAsync(u, 20, Agora, null, default)).Relatorios);
        Assert.Equal("indisponivel", resumo.Estado); Assert.Null(resumo.DiasComConclusao);
        await Assert.ThrowsAsync<RelatorioSalvoIndisponivelException>(() => repo.ConsultarAsync(u, r.Id, default));
    }

}
