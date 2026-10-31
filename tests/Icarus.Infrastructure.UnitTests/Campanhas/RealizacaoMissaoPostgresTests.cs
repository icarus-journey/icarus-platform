using Icarus.Application.Exceptions;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.ConsultarMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class RealizacaoMissaoPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 31, 1, 10, 30, TimeSpan.Zero);
    private static readonly DateOnly Hoje = new(2026, 10, 30);
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private static ConcluirMissaoUseCase Conclusao(IcarusDbContext c, TimeProvider? relogio = null) => new(new MissaoRepository(c), new OcorrenciaRepository(c),
        new MovimentacaoPontosRepository(c), new CampanhaRepository(c), new UsuarioRepository(c), c, new TransacaoConclusaoMissao(c),
        NullLogger<ConcluirMissaoUseCase>.Instance, relogio ?? new Relogio());
    private static ConsultarMissaoUseCase Consulta(IcarusDbContext c) => new(new PlanejamentoMissaoRepository(c), c, new Relogio());

    private async Task<(Guid Usuario, long Missao, long Ocorrencia)> Preparar(DateOnly? prevista = null, bool recorrente = false)
    {
        var u = await banco.CriarUsuarioAsync(100);
        await using var c = banco.NovoContexto();
        (await c.Usuarios.SingleAsync(x => x.Id == u)).FusoHorario = "America/Sao_Paulo";
        var m = new Missao
        {
            UsuarioId = u,
            Titulo = "Praticar",
            Pontuacao = 30,
            DataLimite = prevista ?? Hoje,
            Recorrencia = recorrente ? new() { Tipo = "DIARIA", DataInicio = Hoje.AddDays(-30), Regra = "{}" } : null,
            Ocorrencias = [new() { DataPrevista = prevista ?? Hoje }]
        };
        c.Missoes.Add(m); await c.SaveChangesAsync(); return (u, m.Id, m.Ocorrencias.Single().Id);
    }

    [FatoPostgres]
    public async Task SemCamposUsaAgoraEGuardaFusoSeparadoDoInstante()
    {
        var p = await Preparar();
        await using (var c = banco.NovoContexto())
        {
            var r = await Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia), default);
            Assert.Equal("1.3.0", r.VersaoContrato); Assert.Equal(Agora, r.RealizadaEm); Assert.Equal(Agora, r.ConcluidaEm);
            Assert.Equal("America/Sao_Paulo", r.FusoHorarioRealizacao); Assert.Equal(p.Ocorrencia, r.OcorrenciaId);
            Assert.Equal(130, r.SaldoPontosAtual); Assert.False(r.ConcluidaComAtraso);
        }
        await using var leitura = banco.NovoContexto();
        var o = await leitura.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia);
        Assert.Equal(Agora, o.RealizadaEm); Assert.Equal(Agora, o.ConcluidaEm); Assert.Equal("America/Sao_Paulo", o.FusoHorarioRealizacao);
    }

    [FatoPostgres]
    public async Task RealizadaOntemRegistradaHojeUsaRealizacaoParaAtrasoEIndicadores()
    {
        var ontem = Hoje.AddDays(-1); var p = await Preparar(ontem);
        await using (var c = banco.NovoContexto())
        {
            var r = await Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, ontem, new(20, 0), ontem), default);
            Assert.Equal(new DateTimeOffset(2026, 10, 29, 23, 0, 0, TimeSpan.Zero), r.RealizadaEm);
            Assert.Equal(Agora, r.ConcluidaEm); Assert.False(r.ConcluidaComAtraso);
        }
        await using var leitura = banco.NovoContexto();
        var o = await leitura.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia);
        Assert.Equal(StatusOcorrencia.Concluida, o.Status); Assert.Equal(ontem, o.DataPrevista);
        var dados = await Consulta(leitura).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao), default);
        Assert.Equal(o.RealizadaEm, dados.Execucao.RealizadaEm); Assert.Equal(o.ConcluidaEm, dados.Execucao.ConcluidaEm);
        var indicadores = new IndicadoresPessoaisRepository(leitura);
        Assert.Equal(1, (await indicadores.CalcularAsync(p.Usuario, new(ontem, ontem, "America/Sao_Paulo"), default)).Concluidas);
        Assert.Null((await indicadores.CalcularAsync(p.Usuario, new(Hoje, Hoje, "America/Sao_Paulo"), default)).Concluidas);
    }

    [FatoPostgres]
    public async Task RealizacaoTardiaEhCalculadaNoFusoSemUsarODiaDoRegistroUtc()
    {
        var p = await Preparar(Hoje.AddDays(-2), recorrente: true);
        await using var c = banco.NovoContexto();
        var r = await Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje.AddDays(-1), new(23, 30)), default);
        Assert.True(r.ConcluidaComAtraso);
        Assert.Equal(new DateTimeOffset(2026, 10, 30, 2, 30, 0, TimeSpan.Zero), r.RealizadaEm);
        Assert.Equal(StatusOcorrencia.ConcluidaComAtraso, (await c.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia)).Status);
    }

    [FatoPostgres]
    public async Task RealizacaoAutodeclaradaNaoMudaAExecucaoPlanejada()
    {
        var p = await Preparar(recorrente: true);
        await using var c = banco.NovoContexto();
        var r = await Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje.AddDays(-1), new(20, 0), Hoje), default);
        Assert.False(r.ConcluidaComAtraso); Assert.Equal(Hoje, r.DataPrevista);
        Assert.Single(await c.Ocorrencias.Where(o => o.MissaoId == p.Missao).ToListAsync());
    }

    [FatoPostgres]
    public async Task FuturoOuCamposIncompletosNaoGravamConclusaoNemCredito()
    {
        var p = await Preparar();
        await using var c = banco.NovoContexto();
        foreach (var comando in new ConcluirMissaoCommand[] {
            new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje, new(22, 11)),
            new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje.AddDays(1), new(0, 0)),
            new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje),
            new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, HorarioRealizacao: new(20, 0)) })
            await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(comando, default));
        Assert.Null((await c.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia)).RealizadaEm);
        Assert.Null((await c.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia)).ConcluidaEm);
        Assert.Empty(await c.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
        Assert.Equal(100, (await c.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
    }

    [FatoPostgres]
    public async Task DataEsperadaDivergenteRecusaConfirmacaoSemSelecionarOutraExecucao()
    {
        var p = await Preparar(); await using var c = banco.NovoContexto();
        var erro = await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao,
            DataRealizacao: Hoje, HorarioRealizacao: new(20, 0), DataPrevistaEsperada: Hoje.AddDays(-1)), default));
        Assert.Equal("execucao_alterada", erro.Codigo); Assert.True(erro.Conflito);
        Assert.Empty(await c.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task RetryDevolveOriginalMesmoComOutroMomentoEFusoAtualAlterado()
    {
        var p = await Preparar(); DateTimeOffset? realizada;
        await using (var c = banco.NovoContexto())
        {
            realizada = (await Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje, new(20, 0)), default)).RealizadaEm;
            (await c.Usuarios.SingleAsync(u => u.Id == p.Usuario)).FusoHorario = "Asia/Tokyo"; await c.SaveChangesAsync();
        }
        await using var repeticao = banco.NovoContexto();
        var r = await Conclusao(repeticao).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje.AddYears(1), new(20, 0)), default);
        Assert.True(r.Repetida); Assert.Equal(realizada, r.RealizadaEm); Assert.Equal(Agora, r.ConcluidaEm);
        Assert.Equal("America/Sao_Paulo", r.FusoHorarioRealizacao); Assert.Equal(130, r.SaldoPontosAtual);
        Assert.Single(await repeticao.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
        var dados = await Consulta(repeticao).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao), default);
        Assert.Equal("America/Sao_Paulo", dados.Execucao.FusoHorarioRealizacao);
    }

    [FatoPostgres]
    public async Task PersistenciaImpedeReescreverRealizacaoRegistroEFusoDepoisDaConfirmacao()
    {
        var p = await Preparar();
        await using (var c = banco.NovoContexto()) await Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia), default);
        foreach (var campo in new[] { "realizacao", "registro", "fuso" })
        {
            await using var c = banco.NovoContexto(); var o = await c.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia);
            if (campo == "realizacao") o.RealizadaEm = Agora.AddDays(-1);
            else if (campo == "registro") o.ConcluidaEm = null;
            else o.FusoHorarioRealizacao = "UTC";
            await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        }
        await using var leitura = banco.NovoContexto(); var final = await leitura.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia);
        Assert.Equal(Agora, final.RealizadaEm); Assert.Equal(Agora, final.ConcluidaEm); Assert.Equal("America/Sao_Paulo", final.FusoHorarioRealizacao);
    }

    [FatoPostgres]
    public async Task RealizacaoInformadaNaoContornaFuturoOuIsolamento()
    {
        var p = await Preparar(Hoje.AddDays(1)); var outro = await banco.CriarUsuarioAsync(0);
        await using var c = banco.NovoContexto();
        await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia, Hoje, new(20, 0)), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Conclusao(c).ExecuteAsync(new(outro, p.Usuario, p.Missao, p.Ocorrencia, Hoje, new(20, 0)), default));
        Assert.Empty(await c.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }
    [FatoPostgres]
    public async Task MudancaDeFusoDuranteFormularioNaoReinterpretaHorarioSilenciosamente()
    {
        var p = await Preparar(); await using var c = banco.NovoContexto();
        var erro = await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missao,
            p.Ocorrencia, Hoje, new(20, 0), Hoje, "Asia/Tokyo"), default));
        Assert.Equal("fuso_alterado", erro.Codigo); Assert.True(erro.Conflito);
        Assert.Null((await c.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencia)).ConcluidaEm);
        Assert.Empty(await c.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }

    private sealed class RelogioFracionado : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora.AddTicks(9); }

    [FatoPostgres]
    public async Task PrimeiraRespostaEReplaySaoIdenticosAposPersistenciaComFracaoDeMicrossegundo()
    {
        foreach (var manual in new[] { false, true })
        {
            var p = await Preparar();
            var comando = new ConcluirMissaoCommand(p.Usuario, p.Usuario, p.Missao, p.Ocorrencia,
                manual ? Hoje : null, manual ? new TimeOnly(20, 0).Add(TimeSpan.FromTicks(9)) : null);
            ConcluirMissaoResult original;
            await using (var c = banco.NovoContexto()) original = await Conclusao(c, new RelogioFracionado()).ExecuteAsync(comando, default);
            await using var repeticao = banco.NovoContexto();
            var r = await Conclusao(repeticao, new RelogioFracionado()).ExecuteAsync(comando, default);
            Assert.True(r.Repetida); Assert.Equal(original.RealizadaEm, r.RealizadaEm); Assert.Equal(original.ConcluidaEm, r.ConcluidaEm);
            Assert.Equal(0, r.RealizadaEm!.Value.Ticks % 10); Assert.Equal(0, r.ConcluidaEm.Ticks % 10);
        }
    }

}
