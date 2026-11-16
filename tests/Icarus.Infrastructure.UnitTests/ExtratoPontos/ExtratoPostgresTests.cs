using Icarus.Application.UseCases.ConsultarExtratoPontos;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Icarus.Infrastructure.UnitTests.ExtratoPontos;

public sealed class ExtratoPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Referencia = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly FiltroExtratoPontos Tudo = new(null, null, true, NaturezaExtrato.Todos, 2);
    private sealed class Relogio(DateTimeOffset data) : TimeProvider { public override DateTimeOffset GetUtcNow() => data; }
    private async Task<MovimentacaoPontos> Lancar(Guid pessoa, DateTimeOffset data, long quantidade, long? saldo = null)
    {
        await using var c = new IcarusDbContext(new DbContextOptionsBuilder<IcarusDbContext>()
            .UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention().Options, new Relogio(data));
        var m = new MovimentacaoPontos
        {
            UsuarioId = pessoa,
            Tipo = TipoMovimentacaoPontos.Ajuste,
            Quantidade = quantidade,
            SaldoResultante = saldo,
            CriadoEm = data
        };
        c.MovimentacoesPontos.Add(m); await c.SaveChangesAsync(); return m;
    }

    [FatoPostgres]
    public async Task PaginaPorDataEIdSemRepetirOuIncluirInsercaoEntrePaginas()
    {
        var pessoa = await banco.CriarUsuarioAsync(870);
        var outro = await banco.CriarUsuarioAsync(999);
        var antigos = new List<MovimentacaoPontos>();
        for (var i = 0; i < 5; i++) antigos.Add(await Lancar(pessoa, Referencia.AddHours(-1), 30));
        var alheio = await Lancar(outro, Referencia, 99);
        await using var c = banco.NovoContexto(); var repo = new ExtratoPontosRepository(c);
        var primeira = await repo.ListarAsync(pessoa, Tudo, Referencia, "UTC", null, default);
        Assert.Equal(870, primeira.SaldoAtual); Assert.True(primeira.TemMais);
        Assert.Equal(antigos.TakeLast(2).Reverse().Select(m => m.Id), primeira.Movimentos.Select(m => m.Id));
        await Lancar(pessoa, Referencia.AddHours(-2), 50, 920);
        var ids = primeira.Movimentos.Select(m => m.Id).ToList(); var pagina = primeira;
        while (pagina.TemMais)
        {
            var ultimo = pagina.Movimentos[^1];
            var cursor = new CursorExtratoPontos(pessoa, Tudo, ultimo.RegistradoEm, ultimo.Id, primeira.LimiteId, Referencia, "UTC");
            pagina = await repo.ListarAsync(pessoa, Tudo, Referencia, "UTC", cursor, default);
            ids.AddRange(pagina.Movimentos.Select(m => m.Id));
        }
        Assert.Equal(antigos.AsEnumerable().Reverse().Select(m => m.Id), ids);
        Assert.Null(await repo.ConsultarAsync(pessoa, alheio.Id, "UTC", default));
        Assert.All(primeira.Movimentos, m => Assert.Null(m.SaldoApos));
    }

    [FatoPostgres]
    public async Task CombinaDiaCivilENaturezaSemFiltrarSaldoAtualNemTransformarEstornoEmConquista()
    {
        var pessoa = await banco.CriarUsuarioAsync(400);
        await Lancar(pessoa, new(2026, 10, 20, 2, 59, 59, TimeSpan.Zero), 12);
        var inicio = await Lancar(pessoa, new(2026, 10, 20, 3, 0, 0, TimeSpan.Zero), 20, 20);
        var debito = await Lancar(pessoa, new(2026, 10, 21, 2, 59, 59, TimeSpan.Zero), -10, 10);
        await Lancar(pessoa, new(2026, 10, 21, 3, 0, 0, TimeSpan.Zero), 50, 60);
        await using var c = banco.NovoContexto(); var repo = new ExtratoPontosRepository(c);
        var filtro = new FiltroExtratoPontos(new(2026, 10, 20), new(2026, 10, 20), false, NaturezaExtrato.Todos, 20);
        var p = await repo.ListarAsync(pessoa, filtro, Referencia, "America/Sao_Paulo", null, default);
        Assert.Equal(new[] { debito.Id, inicio.Id }, p.Movimentos.Select(m => m.Id)); Assert.Equal(400, p.SaldoAtual);
        Assert.All(p.Movimentos, m => Assert.Equal(new DateOnly(2026, 10, 20), m.DataLocal));
        var usados = await repo.ListarAsync(pessoa, filtro with { Natureza = NaturezaExtrato.Usados }, Referencia, "America/Sao_Paulo", null, default);
        Assert.Equal(debito.Id, Assert.Single(usados.Movimentos).Id);
        var recebidos = await repo.ListarAsync(pessoa, filtro with { Natureza = NaturezaExtrato.Recebidos }, Referencia, "America/Sao_Paulo", null, default);
        Assert.Equal(inicio.Id, Assert.Single(recebidos.Movimentos).Id);
        var vazio = await repo.ListarAsync(pessoa, filtro with { Inicio = new(2001, 1, 1), Fim = new(2001, 1, 1) }, Referencia, "UTC", null, default);
        Assert.Empty(vazio.Movimentos); Assert.True(vazio.LimiteId > 0); Assert.Equal(400, vazio.SaldoAtual);
    }

    private sealed class AposLeituraSaldo(Func<Task> escrever) : DbCommandInterceptor
    {
        private bool executou;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken ct = default)
        {
            if (!executou && command.CommandText.Contains("saldo_pontos", StringComparison.Ordinal))
            { executou = true; await escrever(); }
            return result;
        }
    }
    [FatoPostgres]
    public async Task LeSaldoEMovimentosNoMesmoSnapshotComCreditoConcorrente()
    {
        var pessoa = await banco.CriarUsuarioAsync(30);
        var antigo = await Lancar(pessoa, Referencia.AddHours(-1), 30, 30);
        var interceptor = new AposLeituraSaldo(async () =>
        {
            await using var escrita = new IcarusDbContext(new DbContextOptionsBuilder<IcarusDbContext>()
                .UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention().Options, new Relogio(Referencia));
            (await escrita.Usuarios.SingleAsync(u => u.Id == pessoa)).SaldoPontos = 60;
            escrita.MovimentacoesPontos.Add(new() { UsuarioId = pessoa, Tipo = TipoMovimentacaoPontos.Ajuste, Quantidade = 30, SaldoResultante = 60, CriadoEm = Referencia });
            await escrita.SaveChangesAsync();
        });
        await using var c = new IcarusDbContext(new DbContextOptionsBuilder<IcarusDbContext>().UseNpgsql(banco.Conexao)
            .UseSnakeCaseNamingConvention().AddInterceptors(interceptor).Options);
        var p = await new ExtratoPontosRepository(c).ListarAsync(pessoa, Tudo, Referencia, "UTC", null, default);
        Assert.Equal(30, p.SaldoAtual); Assert.Equal(antigo.Id, Assert.Single(p.Movimentos).Id);
        var atualizado = await new ExtratoPontosRepository(c).ListarAsync(pessoa, Tudo, Referencia, "UTC", null, default);
        Assert.Equal(60, atualizado.SaldoAtual); Assert.Equal(2, atualizado.Movimentos.Count);
    }
}
