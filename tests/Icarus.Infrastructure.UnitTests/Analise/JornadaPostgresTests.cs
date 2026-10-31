using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.ConsultarJornada;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Icarus.Infrastructure.UnitTests.Analise;

public sealed class JornadaPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.Parse("2026-10-31T15:00:00Z", CultureInfo.InvariantCulture);
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private static ConsultarJornadaUseCase Consulta(IcarusDbContext contexto) => new(new JornadaRepository(contexto), new Relogio());

    private async Task<Guid> Pessoa(long saldo = 50)
    {
        var id = await banco.CriarUsuarioAsync(saldo);
        await using var c = banco.NovoContexto();
        (await c.Usuarios.FindAsync(id))!.FusoHorario = "America/Sao_Paulo";
        await c.SaveChangesAsync();
        return id;
    }

    private static void Registrar(IcarusDbContext c, Guid id, int dia, string? fuso = "America/Sao_Paulo", StatusOcorrencia status = StatusOcorrencia.Concluida, StatusMissao estado = StatusMissao.Ativa)
    {
        var missao = new Missao { UsuarioId = id, Titulo = "Texto pessoal não deve sair", Pontuacao = 20, Horario = Agora, Status = estado };
        c.Ocorrencias.Add(new()
        {
            Missao = missao,
            DataPrevista = new(2026, 10, 1),
            Status = status,
            RealizadaEm = new(2026, 10, dia, 12, 0, 0, TimeSpan.Zero),
            ConcluidaEm = Agora,
            FusoHorarioRealizacao = fuso
        });
    }

    [FatoPostgres]
    public async Task IsolaContasEPreservaConclusoesDeMissoesExcluidasSemAlterarDados()
    {
        var id = await Pessoa(1270); var outro = await Pessoa(900);
        await using (var c = banco.NovoContexto())
        {
            Registrar(c, id, 29, estado: StatusMissao.Excluida);
            Registrar(c, id, 30, status: StatusOcorrencia.ConcluidaComAtraso, estado: StatusMissao.Inativa);
            Registrar(c, id, 31); Registrar(c, id, 31); Registrar(c, id, 31);
            Registrar(c, outro, 28); Registrar(c, id, 28, status: StatusOcorrencia.Excluida);
            await c.SaveChangesAsync();
        }
        await using var leitura = banco.NovoContexto();
        var antes = await leitura.Ocorrencias.CountAsync();
        var resultado = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal(1270, resultado.SaldoPontos);
        Assert.Equal(3, resultado.Sequencia.DiasAtuais);
        Assert.True(resultado.Sequencia.RealizouHoje);
        Assert.DoesNotContain("Texto pessoal", JsonSerializer.Serialize(resultado));
        Assert.Equal(antes, await leitura.Ocorrencias.CountAsync());
        Assert.Empty(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == id).ToListAsync());
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Consulta(leitura).ExecuteAsync(new(outro, id), default));
        Assert.Equal(0, (await Consulta(leitura).ExecuteAsync(new(outro, outro), default)).Sequencia.DiasAtuais);
    }

    [FatoPostgres]
    public async Task NovaConsultaRecalculaRetroativoSemDuplicarDiaOuAlterarSaldo()
    {
        var id = await Pessoa();
        await using (var c = banco.NovoContexto())
        {
            Registrar(c, id, 29); Registrar(c, id, 31);
            await c.SaveChangesAsync();
        }
        await using var leitura = banco.NovoContexto();
        Assert.Equal(1, (await Consulta(leitura).ExecuteAsync(new(id, id), default)).Sequencia.DiasAtuais);
        await using (var c = banco.NovoContexto())
        {
            Registrar(c, id, 30); Registrar(c, id, 30);
            await c.SaveChangesAsync();
        }
        var depois = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal(3, depois.Sequencia.DiasAtuais);
        Assert.Equal(50, depois.SaldoPontos);
        Assert.Equal(3, depois.Sequencia.Dias.Count(d => d.Realizada == true));
    }

    [FatoPostgres]
    public async Task FusoAntigoPermaneceAOrigemDaDataDepoisDeAlterarPerfil()
    {
        var id = await Pessoa();
        await using (var c = banco.NovoContexto())
        {
            Registrar(c, id, 30);
            var evento = c.ChangeTracker.Entries<Ocorrencia>().Single().Entity;
            evento.RealizadaEm = new(2026, 10, 31, 1, 0, 0, TimeSpan.Zero);
            Registrar(c, id, 31, "Asia/Tokyo");
            (await c.Usuarios.FindAsync(id))!.FusoHorario = "Asia/Tokyo";
            await c.SaveChangesAsync();
        }
        await using var leitura = banco.NovoContexto();
        var resultado = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal(2, resultado.Sequencia.DiasAtuais);
        Assert.Equal("Asia/Tokyo", resultado.Sequencia.FusoHorario);
    }

    [FatoPostgres]
    public async Task LegadoSemMetadadosContinuaAusenteESemBackfill()
    {
        var id = await Pessoa();
        await using (var c = banco.NovoContexto())
        {
            Registrar(c, id, 30, null);
            Registrar(c, id, 31);
            c.Missoes.Add(new() { UsuarioId = id, Titulo = "Legado sem execução", Horario = Agora, ConcluidaEm = Agora });
            await c.SaveChangesAsync();
        }
        await using var leitura = banco.NovoContexto();
        var resultado = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal("historico_incompleto", resultado.Sequencia.Estado);
        Assert.Null(resultado.Sequencia.DiasAtuais);
        Assert.True(resultado.Sequencia.RealizouHoje);
        Assert.Null(await leitura.Ocorrencias.Where(o => o.Missao.UsuarioId == id && o.RealizadaEm!.Value.Day == 30).Select(o => o.FusoHorarioRealizacao).SingleAsync());
        Assert.Equal(2, await leitura.Ocorrencias.CountAsync(o => o.Missao.UsuarioId == id));
    }

    [FatoPostgres]
    public async Task LeituraNaoReaproveitaSaldoRastreadoAntesDeUmaConclusao()
    {
        var id = await Pessoa(50);
        await using var leitura = banco.NovoContexto();
        await leitura.Usuarios.FindAsync(id);
        await using (var c = banco.NovoContexto())
        {
            (await c.Usuarios.FindAsync(id))!.SaldoPontos = 70;
            Registrar(c, id, 31);
            await c.SaveChangesAsync();
        }
        var resultado = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal(70, resultado.SaldoPontos);
        Assert.Equal(1, resultado.Sequencia.DiasAtuais);
    }

    private sealed class PausaAposSaldo : DbCommandInterceptor
    {
        public TaskCompletionSource SaldoLido { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _pausou;
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!_pausou && command.CommandText.Contains("saldo_pontos", StringComparison.Ordinal))
            {
                _pausou = true;
                SaldoLido.SetResult();
                await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    [FatoPostgres]
    public async Task ConclusaoEntreAsConsultasNaoMisturaSaldoAntigoESequenciaNova()
    {
        var id = await Pessoa(50);
        var pausa = new PausaAposSaldo();
        await using var leitura = new IcarusDbContext(new DbContextOptionsBuilder<IcarusDbContext>()
            .UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention().AddInterceptors(pausa).Options);
        var consulta = Consulta(leitura).ExecuteAsync(new(id, id), default);
        try
        {
            await pausa.SaldoLido.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await using var c = banco.NovoContexto();
            (await c.Usuarios.FindAsync(id))!.SaldoPontos = 70;
            Registrar(c, id, 31);
            await c.SaveChangesAsync();
        }
        finally { pausa.Continuar.TrySetResult(); }
        var resultado = await consulta;
        Assert.Equal(50, resultado.SaldoPontos);
        Assert.Equal(0, resultado.Sequencia.DiasAtuais);
        var nova = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal(70, nova.SaldoPontos);
        Assert.Equal(1, nova.Sequencia.DiasAtuais);
    }
}
