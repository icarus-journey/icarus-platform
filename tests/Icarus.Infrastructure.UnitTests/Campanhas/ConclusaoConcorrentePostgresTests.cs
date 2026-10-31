using System.Diagnostics;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Application.UseCases.ResgatarRecompensa;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class ConclusaoConcorrentePostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Agora = new(DateTimeOffset.UtcNow.Ticks / 10 * 10, TimeSpan.Zero);
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(Agora.UtcDateTime);
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private sealed record Planejamento(Guid Usuario, long[] Missoes, long[] Ocorrencias, long? Campanha);

    private IcarusDbContext Contexto(string nome)
    {
        var conexao = new NpgsqlConnectionStringBuilder(banco.Conexao) { ApplicationName = nome };
        return new(new DbContextOptionsBuilder<IcarusDbContext>().UseNpgsql(conexao.ConnectionString).UseSnakeCaseNamingConvention().Options);
    }
    private static ConcluirMissaoUseCase Conclusao(IcarusDbContext c, IUnitOfWork? unidade = null)
        => new(new MissaoRepository(c), new OcorrenciaRepository(c), new MovimentacaoPontosRepository(c), new CampanhaRepository(c),
            new UsuarioRepository(c), unidade ?? c, new TransacaoConclusaoMissao(c), NullLogger<ConcluirMissaoUseCase>.Instance, new Relogio());
    private static ResgatarRecompensaUseCase Resgate(IcarusDbContext c, IUnitOfWork? unidade = null)
    {
        var repositorio = new LojaRepository(c); var opcoes = new LojaOptions { CustoProtecao = 500 };
        return new(repositorio, unidade ?? c, new ConsultarLojaUseCase(repositorio, opcoes), opcoes, NullLogger<ResgatarRecompensaUseCase>.Instance);
    }
    private async Task<Planejamento> Preparar(int quantidade = 1, bool recorrente = false, bool semOcorrencia = false, bool campanha = false, string unidade = "pontos")
    {
        var usuario = await banco.CriarUsuarioAsync(100);
        await using var c = banco.NovoContexto();
        (await c.Usuarios.SingleAsync(u => u.Id == usuario)).FusoHorario = "UTC";
        var vinculada = campanha ? new Campanha
        {
            Titulo = "Objetivo de teste",
            Epico = new() { UsuarioId = usuario, Titulo = "Estudos" },
            Estado = EstadoCampanha.EmAndamento,
            PrazoInicio = Hoje.AddDays(-30),
            PrazoFim = Hoje.AddDays(30),
            ValorAtual = 0,
            ValorMensurado = 500,
            Unidade = unidade
        } : null;
        var missoes = Enumerable.Range(1, quantidade).Select(i => new Missao
        {
            UsuarioId = usuario,
            Titulo = $"Missão {i}",
            Pontuacao = 30,
            Dificuldade = Dificuldade.Facil,
            Campanha = vinculada,
            DataLimite = Hoje,
            Recorrencia = recorrente ? new() { Tipo = "DIARIA", DataInicio = Hoje.AddDays(-30), Regra = "{}" } : null,
            Ocorrencias = semOcorrencia ? [] : [new() { DataPrevista = Hoje }]
        }).ToArray();
        c.Missoes.AddRange(missoes); await c.SaveChangesAsync();
        return new(usuario, missoes.Select(m => m.Id).ToArray(), missoes.SelectMany(m => m.Ocorrencias.Select(o => o.Id)).ToArray(), vinculada?.Id);
    }
    private async Task<long> Recompensa(Guid usuario)
    {
        await using var c = banco.NovoContexto();
        var item = new Item { UsuarioId = usuario, Nome = "Pausa de teste", Valor = 60, ControlaApp = ControlaApp.Nao };
        c.Itens.Add(item); await c.SaveChangesAsync(); return item.Id;
    }
    private async Task<ConcluirMissaoResult> Executar(Planejamento p, int indice = 0, bool identificar = true)
    {
        await using var c = banco.NovoContexto();
        return await Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[indice], identificar ? p.Ocorrencias[indice] : null), default);
    }
    private sealed class PausarNoSalvar(IUnitOfWork unidade) : IUnitOfWork
    {
        public TaskCompletionSource<bool> Entrou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task SalvarAlteracoesAsync(CancellationToken ct)
        {
            Entrou.TrySetResult(true);
            await Continuar.Task.WaitAsync(TimeSpan.FromSeconds(20), ct);
            await unidade.SalvarAlteracoesAsync(ct);
        }
    }
    private async Task<bool> EsperarBloqueio(string nome)
    {
        await using var conexao = new NpgsqlConnection(banco.Conexao); await conexao.OpenAsync();
        var cronometro = Stopwatch.StartNew();
        while (cronometro.Elapsed < TimeSpan.FromSeconds(8))
        {
            await using var comando = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE datname=current_database() AND application_name=@nome AND wait_event_type='Lock')", conexao);
            comando.Parameters.AddWithValue("nome", nome);
            if ((bool)(await comando.ExecuteScalarAsync())!) return true;
            await Task.Delay(25);
        }
        return false;
    }

    [FatoPostgres]
    public async Task MesmaExecucaoAguardaCommitEDevolveRespostaOriginalSemNovoCredito()
    {
        var p = await Preparar(); var nome = "espera_conclusao_" + Guid.NewGuid().ToString("N");
        await using var c1 = banco.NovoContexto(); await using var c2 = Contexto(nome);
        var pausa = new PausarNoSalvar(c1);
        var primeira = Conclusao(c1, pausa).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0]), default);
        await pausa.Entrou.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var segunda = Conclusao(c2).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0], Hoje.AddDays(-1), new(20, 0)), default);
        bool bloqueou;
        try { bloqueou = await EsperarBloqueio(nome); }
        finally { pausa.Continuar.TrySetResult(true); }
        var resultados = await Task.WhenAll(primeira, segunda);
        Assert.True(bloqueou); Assert.False(resultados[0].Repetida); Assert.True(resultados[1].Repetida);
        Assert.Equal(resultados[0].RealizadaEm, resultados[1].RealizadaEm); Assert.Equal(resultados[0].ConcluidaEm, resultados[1].ConcluidaEm);
        Assert.Equal(130, resultados[1].SaldoPontosAtual);
        await using var leitura = banco.NovoContexto();
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
        Assert.Equal(130, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
    }

    [FatoPostgres]
    public async Task DezesseisChamadasDaMesmaExecucaoGeramUmCreditoUmaConclusaoEQuinzeRepeticoes()
    {
        var p = await Preparar(recorrente: true);
        var resultados = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Executar(p)));
        Assert.Single(resultados, r => !r.Repetida); Assert.Equal(15, resultados.Count(r => r.Repetida));
        Assert.Single(resultados.Select(r => r.RealizadaEm).Distinct()); Assert.Single(resultados.Select(r => r.ConcluidaEm).Distinct());
        Assert.All(resultados, r => Assert.Equal(130, r.SaldoPontosAtual));
        await using var leitura = banco.NovoContexto();
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
        Assert.Equal(1, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == p.Missoes[0] && o.ConcluidaEm != null));
    }

    [FatoPostgres]
    public async Task MissoesDiferentesSomamIntegralmenteSaldoEContadorDaCampanha()
    {
        var p = await Preparar(16, campanha: true);
        var resultados = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => Executar(p, i)));
        Assert.All(resultados, r => Assert.False(r.Repetida));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(580, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
        Assert.Equal(480, (await leitura.Campanhas.SingleAsync(c => c.Id == p.Campanha)).ValorAtual);
        Assert.Equal(16, await leitura.MovimentacoesPontos.CountAsync(m => m.UsuarioId == p.Usuario));
        Assert.Equal(16, await leitura.Ocorrencias.CountAsync(o => o.Missao.UsuarioId == p.Usuario && o.ConcluidaEm != null));
    }

    [FatoPostgres]
    public async Task ConcorrenciaNaoConvertePontosEmResultadoMensurado()
    {
        var p = await Preparar(4, campanha: true, unidade: "módulos");
        await Task.WhenAll(Enumerable.Range(0, 4).Select(i => Executar(p, i)));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(220, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
        Assert.Equal(0, (await leitura.Campanhas.SingleAsync(c => c.Id == p.Campanha)).ValorAtual);
        Assert.Empty(await leitura.MedicoesCampanhas.Where(m => m.CampanhaId == p.Campanha).ToListAsync());
    }

    [FatoPostgres]
    public async Task LegadoSemOcorrenciaMaterializaUmaUnicaLinhaMesmoSobConcorrencia()
    {
        foreach (var recorrente in new[] { false, true })
        {
            var p = await Preparar(recorrente: recorrente, semOcorrencia: true);
            var resultados = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Executar(p, identificar: false)));
            Assert.Single(resultados, r => !r.Repetida);
            await using var leitura = banco.NovoContexto();
            Assert.Single(await leitura.Ocorrencias.Where(o => o.MissaoId == p.Missoes[0]).ToListAsync());
            Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
            Assert.Equal(130, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
        }
    }

    [FatoPostgres]
    public async Task ConcluirAtrasoIdentificadoNaoConcluiOutroDiaPendenteAoRepetir()
    {
        var p = await Preparar(recorrente: true);
        await using (var c = banco.NovoContexto())
        {
            (await c.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencias[0])).DataPrevista = Hoje.AddDays(-3);
            c.Ocorrencias.AddRange(new Ocorrencia { MissaoId = p.Missoes[0], DataPrevista = Hoje.AddDays(-1) }, new Ocorrencia { MissaoId = p.Missoes[0], DataPrevista = Hoje.AddDays(1) });
            await c.SaveChangesAsync();
        }
        var resultados = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Executar(p)));
        Assert.Single(resultados, r => !r.Repetida);
        await using var leitura = banco.NovoContexto();
        Assert.Equal(2, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == p.Missoes[0] && o.ConcluidaEm == null));
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task ConclusaoEResgateUsamOMesmoBloqueioDeSaldoNasDuasOrdens()
    {
        foreach (var lojaPrimeiro in new[] { false, true })
        {
            var p = await Preparar(); var item = await Recompensa(p.Usuario); var nome = "espera_loja_" + Guid.NewGuid().ToString("N");
            await using var c1 = banco.NovoContexto(); await using var c2 = Contexto(nome);
            var pausa = new PausarNoSalvar(c1);
            Task primeira = lojaPrimeiro
                ? Resgate(c1, pausa).ExecuteAsync(new(p.Usuario, p.Usuario, item, Guid.NewGuid()), default)
                : Conclusao(c1, pausa).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0]), default);
            await pausa.Entrou.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Task segunda = lojaPrimeiro
                ? Conclusao(c2).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0]), default)
                : Resgate(c2).ExecuteAsync(new(p.Usuario, p.Usuario, item, Guid.NewGuid()), default);
            bool bloqueou;
            try { bloqueou = await EsperarBloqueio(nome); }
            finally { pausa.Continuar.TrySetResult(true); }
            await Task.WhenAll(primeira, segunda); Assert.True(bloqueou);
            await using var leitura = banco.NovoContexto();
            Assert.Equal(70, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
            Assert.Equal(2, await leitura.MovimentacoesPontos.CountAsync(m => m.UsuarioId == p.Usuario));
            Assert.Equal(EstadoRecompensa.Resgatada, (await leitura.Itens.SingleAsync(i => i.Id == item)).Estado);
        }
    }

    private sealed class FalharDepoisDeSalvar(IUnitOfWork unidade) : IUnitOfWork
    {
        public async Task SalvarAlteracoesAsync(CancellationToken ct)
        {
            await unidade.SalvarAlteracoesAsync(ct);
            throw new InvalidOperationException("Falha simulada antes do commit.");
        }
    }

    [FatoPostgres]
    public async Task FalhaDepoisDeSalvarReverteConclusaoCreditoSaldoEContadorDaCampanha()
    {
        var p = await Preparar(campanha: true);
        await using (var c = banco.NovoContexto())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => Conclusao(c, new FalharDepoisDeSalvar(c)).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0]), default));
            Assert.Empty(c.ChangeTracker.Entries());
        }
        await using (var leitura = banco.NovoContexto())
        {
            Assert.Equal(100, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
            Assert.Null((await leitura.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencias[0])).ConcluidaEm);
            Assert.Null((await leitura.Missoes.SingleAsync(m => m.Id == p.Missoes[0])).ConcluidaEm);
            Assert.Equal(0, (await leitura.Campanhas.SingleAsync(c => c.Id == p.Campanha)).ValorAtual);
            Assert.Empty(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
        }
        Assert.Equal(130, (await Executar(p)).SaldoPontosAtual);
    }

    [FatoPostgres]
    public async Task CancelamentoLiberaBloqueioSemGravarConclusaoParcial()
    {
        var p = await Preparar(2); using var cancelamento = new CancellationTokenSource();
        await using var c1 = banco.NovoContexto(); await using var c2 = banco.NovoContexto();
        var pausa = new PausarNoSalvar(c1);
        var primeira = Conclusao(c1, pausa).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0]), cancelamento.Token);
        await pausa.Entrou.Task.WaitAsync(TimeSpan.FromSeconds(10)); cancelamento.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => primeira);
        var segunda = await Conclusao(c2).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[1], p.Ocorrencias[1]), default).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(130, segunda.SaldoPontosAtual);
        await using var leitura = banco.NovoContexto();
        Assert.Null((await leitura.Ocorrencias.SingleAsync(o => o.Id == p.Ocorrencias[0])).ConcluidaEm);
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task ContaDiferenteConcluiSemEsperarOBloqueioDeOutraPessoa()
    {
        var a = await Preparar(); var b = await Preparar(); await using var c = banco.NovoContexto();
        var pausa = new PausarNoSalvar(c);
        var primeira = Conclusao(c, pausa).ExecuteAsync(new(a.Usuario, a.Usuario, a.Missoes[0], a.Ocorrencias[0]), default);
        await pausa.Entrou.Task.WaitAsync(TimeSpan.FromSeconds(10));
        ConcluirMissaoResult outra;
        try { outra = await Executar(b).WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { pausa.Continuar.TrySetResult(true); }
        await primeira; Assert.Equal(130, outra.SaldoPontosAtual);
    }

    [FatoPostgres]
    public async Task ConsultaAntigaNoMesmoContextoNaoReutilizaSaldoOuExecucaoDesatualizados()
    {
        var p = await Preparar(); await using var antigo = banco.NovoContexto();
        await antigo.Usuarios.SingleAsync(u => u.Id == p.Usuario); await antigo.Missoes.Include(m => m.Ocorrencias).SingleAsync(m => m.Id == p.Missoes[0]);
        await Executar(p);
        var repetida = await Conclusao(antigo).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0]), default);
        Assert.True(repetida.Repetida); Assert.Equal(130, repetida.SaldoPontosAtual);
    }

    [FatoPostgres]
    public async Task ContextoComAlteracaoPendenteEhRecusadoSemDescartarMudancaDoChamador()
    {
        var p = await Preparar(); await using var c = banco.NovoContexto();
        var usuario = await c.Usuarios.SingleAsync(u => u.Id == p.Usuario); usuario.Nome = "Alteração pendente";
        await Assert.ThrowsAsync<InvalidOperationException>(() => Conclusao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Missoes[0], p.Ocorrencias[0]), default));
        Assert.True(c.ChangeTracker.HasChanges()); Assert.Equal("Alteração pendente", usuario.Nome);
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task IndiceUnicoRecusaSegundoCreditoMesmoForaDoCasoDeUsoEReverteSaldo()
    {
        var p = await Preparar(); await Executar(p); await using var c = banco.NovoContexto();
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => new TransacaoConclusaoMissao(c).ExecutarAsync(p.Usuario, async ct =>
        {
            (await c.Usuarios.SingleAsync(u => u.Id == p.Usuario, ct)).SaldoPontos += 30;
            c.MovimentacoesPontos.Add(new() { UsuarioId = p.Usuario, OcorrenciaId = p.Ocorrencias[0], Tipo = TipoMovimentacaoPontos.GanhoMissao, Quantidade = 30 });
            await ((IUnitOfWork)c).SalvarAlteracoesAsync(ct); return true;
        }, default));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(130, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }
}
