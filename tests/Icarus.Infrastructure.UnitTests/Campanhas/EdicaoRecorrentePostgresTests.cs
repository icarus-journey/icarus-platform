using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.AtualizarMissao;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.ConsultarMissao;
using Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;
using Icarus.Application.UseCases.ExcluirMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class EdicaoRecorrentePostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private sealed class Relogio(DateTimeOffset agora) : TimeProvider { public override DateTimeOffset GetUtcNow() => agora; }
    private static readonly DateTimeOffset Agora = new(DateTimeOffset.UtcNow.Ticks / 10 * 10, TimeSpan.Zero);
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(Agora.UtcDateTime);
    private static AtualizarMissaoUseCase Edicao(IcarusDbContext c, IUnitOfWork? unidade = null, TimeProvider? relogio = null)
        => new(new PlanejamentoMissaoRepository(c), unidade ?? c, NullLogger<AtualizarMissaoUseCase>.Instance, relogio ?? new Relogio(Agora));
    private static ConcluirMissaoUseCase Conclusao(IcarusDbContext c, IUnitOfWork? unidade = null, TimeProvider? relogio = null)
        => new(new MissaoRepository(c), new OcorrenciaRepository(c), new MovimentacaoPontosRepository(c),
            new CampanhaRepository(c), new UsuarioRepository(c), unidade ?? c, NullLogger<ConcluirMissaoUseCase>.Instance, relogio);
    private static ConsultarMissaoUseCase Consulta(IcarusDbContext c) => new(new PlanejamentoMissaoRepository(c));

    private async Task<(Guid Usuario, long Id)> PrepararAsync(bool vinculada = false, DateOnly? data = null, string fuso = "UTC")
    {
        var hoje = data ?? Hoje;
        var usuario = await banco.CriarUsuarioAsync(500);
        await using var c = banco.NovoContexto();
        (await c.Usuarios.SingleAsync(u => u.Id == usuario)).FusoHorario = fuso;
        var missao = new Missao
        {
            UsuarioId = usuario,
            Titulo = "Ler uma aula",
            Descricao = "Descrição original",
            Beneficio = "Aprender",
            Dificuldade = Dificuldade.Facil,
            Pontuacao = 30,
            Horario = new DateTimeOffset(hoje.ToDateTime(new(9, 0)), TimeSpan.Zero),
            Recorrencia = new() { Tipo = "DIARIA", DataInicio = hoje.AddDays(-30), Regra = "{}" },
            Campanha = vinculada ? new Campanha
            {
                Titulo = "Curso",
                Estado = EstadoCampanha.EmAndamento,
                Epico = new Epico { UsuarioId = usuario, Titulo = "Estudar" },
                PrazoInicio = hoje.AddDays(-1),
                PrazoFim = hoje.AddDays(10),
                ValorMensurado = 10,
                ValorAtual = 2,
                Unidade = "módulos"
            } : null
        };
        missao.Ocorrencias = [
            new() { DataPrevista = hoje.AddDays(-2), Status = StatusOcorrencia.Concluida, ConcluidaEm = Agora, RealizadaEm = Agora },
            new() { DataPrevista = hoje.AddDays(-1), Status = StatusOcorrencia.Atrasada },
            new() { DataPrevista = hoje },
            new() { DataPrevista = hoje.AddDays(1) },
            new() { DataPrevista = hoje.AddDays(2) },
            new() { DataPrevista = hoje.AddDays(3), Status = StatusOcorrencia.ConcluidaComAtraso, ConcluidaEm = Agora, RealizadaEm = Agora }];
        c.Missoes.Add(missao);
        c.MovimentacoesPontos.Add(new() { UsuarioId = usuario, Ocorrencia = missao.Ocorrencias.First(), Quantidade = 30, Tipo = TipoMovimentacaoPontos.GanhoMissao, Descricao = "Crédito original" });
        await c.SaveChangesAsync(); return (usuario, missao.Id);
    }

    private static AtualizarMissaoCommand Dados(Guid usuario, long id, string versao, TipoRecorrencia tipo = TipoRecorrencia.Diaria,
        List<DayOfWeek>? dias = null, DateOnly? fim = null, int? dia = null)
        => new(usuario, usuario, id, "Ler duas aulas", "Nova descrição", "Aprender mais", Dificuldade.Dificil,
            null, "20:30", tipo, dias, fim, dia, versao);

    [FatoPostgres]
    public async Task EdicaoPreservaConclusoesLancamentosESaldoEAplicaPontosNovosSomenteAmanha()
    {
        var (usuario, id) = await PrepararAsync(vinculada: true);
        long concluidaId; DateTimeOffset concluidaAtualizada;
        await using (var c = banco.NovoContexto())
        {
            var concluida = await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje.AddDays(-2));
            concluidaId = concluida.Id; concluidaAtualizada = concluida.AtualizadoEm;
            var dados = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            Assert.True(dados.PodeEditar); Assert.Equal(TipoRecorrencia.Diaria, dados.ConfiguracaoRecorrencia!.Tipo);
            var atualizada = await Edicao(c).ExecuteAsync(Dados(usuario, id, dados.Versao), default);
            Assert.Equal(60, atualizada.Pontuacao); Assert.Equal(Hoje.AddDays(1), atualizada.ConfiguracaoRecorrencia!.AlteracoesAPartirDe);
        }
        await using var leitura = banco.NovoContexto();
        var antiga = await leitura.Ocorrencias.SingleAsync(o => o.Id == concluidaId);
        Assert.Null(antiga.Planejamento); Assert.Equal(concluidaAtualizada, antiga.AtualizadoEm); Assert.Equal(Agora, antiga.ConcluidaEm);
        foreach (var data in new[] { Hoje.AddDays(-1), Hoje })
        {
            var ocorrencia = await leitura.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == data);
            Assert.Null(ocorrencia.Planejamento);
            var missao = await leitura.Missoes.Include(m => m.Recorrencia).SingleAsync(m => m.Id == id);
            Assert.Equal(30, PlanejamentoOcorrencia.DaExecucao(missao, ocorrencia)!.Pontuacao);
            Assert.Equal("Ler uma aula", PlanejamentoOcorrencia.DaExecucao(missao, ocorrencia)!.Titulo);
        }
        Assert.Equal(60, PlanejamentoOcorrencia.Ler(await leitura.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje.AddDays(1)))!.Pontuacao);
        Assert.Equal(500, (await leitura.Usuarios.SingleAsync(u => u.Id == usuario)).SaldoPontos);
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).ToListAsync());
        Assert.Equal(2, (await leitura.Campanhas.SingleAsync(x => x.Epico.UsuarioId == usuario)).ValorAtual);
        var conclusao = await Conclusao(leitura).ExecuteAsync(new(usuario, usuario, id), default);
        Assert.Equal(30, conclusao.PontuacaoCreditada); Assert.Equal(530, conclusao.SaldoPontosAtual);
        Assert.Equal(30, (await Conclusao(leitura).ExecuteAsync(new(usuario, usuario, id), default)).PontuacaoCreditada);
        Assert.Contains("Ler uma aula", (await leitura.MovimentacoesPontos.OrderByDescending(m => m.Id).FirstAsync(m => m.UsuarioId == usuario)).Descricao);
    }

    [FatoPostgres]
    public async Task SemanalCancelaSomenteFuturoElegivelEVoltarADiariaReutilizaLinhasSemDuplicar()
    {
        var (usuario, id) = await PrepararAsync();
        await using (var c = banco.NovoContexto())
        {
            var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            await Edicao(c).ExecuteAsync(Dados(usuario, id, atual.Versao, TipoRecorrencia.Semanal, [Hoje.AddDays(2).DayOfWeek], Hoje.AddDays(8)), default);
        }
        long cancelada;
        await using (var c = banco.NovoContexto())
        {
            var linhas = await c.Ocorrencias.Where(o => o.MissaoId == id).ToListAsync();
            cancelada = linhas.Single(o => o.DataPrevista == Hoje.AddDays(1)).Id;
            Assert.Equal(StatusOcorrencia.Excluida, linhas.Single(o => o.Id == cancelada).Status);
            Assert.Equal(StatusOcorrencia.Pendente, linhas.Single(o => o.DataPrevista == Hoje).Status);
            Assert.Equal(StatusOcorrencia.Atrasada, linhas.Single(o => o.DataPrevista == Hoje.AddDays(-1)).Status);
            Assert.Equal(StatusOcorrencia.ConcluidaComAtraso, linhas.Single(o => o.DataPrevista == Hoje.AddDays(3)).Status);
            var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            await Edicao(c).ExecuteAsync(Dados(usuario, id, atual.Versao, fim: Hoje.AddDays(8)), default);
        }
        await using var leitura = banco.NovoContexto();
        Assert.Equal(StatusOcorrencia.Pendente, (await leitura.Ocorrencias.SingleAsync(o => o.Id == cancelada)).Status);
        var finais = await leitura.Ocorrencias.Where(o => o.MissaoId == id).ToListAsync();
        Assert.Equal(finais.Count, finais.Select(o => o.DataPrevista).Distinct().Count());
        Assert.DoesNotContain(finais, o => o.DataPrevista > Hoje.AddDays(8));
        Assert.Equal(30, (await Conclusao(leitura).ExecuteAsync(new(usuario, usuario, id), default)).PontuacaoCreditada);
    }

    [FatoPostgres]
    public async Task MensalRespeitaUltimoDiaDeFevereiroEInicioFimDaCampanha()
    {
        var janeiro = new DateOnly(2027, 1, 30);
        var (usuario, id) = await PrepararAsync(data: janeiro, vinculada: true);
        await using (var c = banco.NovoContexto())
        {
            var campanha = await c.Campanhas.SingleAsync(x => x.Epico.UsuarioId == usuario);
            campanha.PrazoInicio = new(2027, 2, 1); campanha.PrazoFim = new(2027, 2, 28); await c.SaveChangesAsync();
            var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            await Edicao(c, relogio: new Relogio(new(2027, 1, 30, 12, 0, 0, TimeSpan.Zero)))
                .ExecuteAsync(Dados(usuario, id, atual.Versao, TipoRecorrencia.Mensal, dia: 31), default);
        }
        await using var leitura = banco.NovoContexto();
        var futuras = await leitura.Ocorrencias.Where(o => o.MissaoId == id && o.DataPrevista > janeiro && o.Status == StatusOcorrencia.Pendente).ToListAsync();
        Assert.Single(futuras); Assert.Equal(new DateOnly(2027, 2, 28), futuras[0].DataPrevista);
        Assert.Equal(31, (await Consulta(leitura).ExecuteAsync(new(usuario, usuario, id), default)).ConfiguracaoRecorrencia!.DiaDoMes);
    }

    [FatoPostgres]
    public async Task FusoDefineAmanhaSemAnteciparNaViradaUtc()
    {
        var local = new DateOnly(2026, 10, 24);
        var (usuario, id) = await PrepararAsync(data: local, fuso: "America/Sao_Paulo");
        await using var c = banco.NovoContexto();
        var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
        var resultado = await Edicao(c, relogio: new Relogio(new(2026, 10, 25, 1, 0, 0, TimeSpan.Zero)))
            .ExecuteAsync(Dados(usuario, id, atual.Versao), default);
        Assert.Equal(new DateOnly(2026, 10, 25), resultado.ConfiguracaoRecorrencia!.AlteracoesAPartirDe);
        Assert.Null((await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == local)).Planejamento);
        Assert.Equal(60, PlanejamentoOcorrencia.Ler(await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == local.AddDays(1)))!.Pontuacao);
    }

    [FatoPostgres]
    public async Task VersaoInvalidaEConfiguracaoInvalidaNaoAlteramPlanejamento()
    {
        var (usuario, id) = await PrepararAsync();
        await using var c = banco.NovoContexto();
        var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Edicao(c).ExecuteAsync(Dados(usuario, id, "antiga"), default));
        foreach (var invalido in new[] { Dados(usuario, id, atual.Versao, TipoRecorrencia.Semanal, []),
            Dados(usuario, id, atual.Versao, TipoRecorrencia.Mensal, dia: 32), Dados(usuario, id, atual.Versao, fim: Hoje) })
            await Assert.ThrowsAsync<EdicaoMissaoException>(() => Edicao(c).ExecuteAsync(invalido, default));
        Assert.Equal("Ler uma aula", (await c.Missoes.SingleAsync(m => m.Id == id)).Titulo);
        Assert.All(await c.Ocorrencias.Where(o => o.MissaoId == id).ToListAsync(), o => Assert.Null(o.Planejamento));
    }

    [FatoPostgres]
    public async Task InativaEncerradaAlheiaEExcluidaContinuamBloqueadas()
    {
        var (usuario, id) = await PrepararAsync(vinculada: true); var outro = await banco.CriarUsuarioAsync(0);
        await using var c = banco.NovoContexto();
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Consulta(c).ExecuteAsync(new(outro, outro, id), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Edicao(c).ExecuteAsync(Dados(usuario, id, "x") with { UsuarioIdAutenticado = outro }, default));
        var missao = await c.Missoes.Include(m => m.Campanha).SingleAsync(m => m.Id == id);
        missao.Status = StatusMissao.Inativa; await c.SaveChangesAsync();
        Assert.False((await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default)).PodeEditar);
        await Assert.ThrowsAsync<MissaoInativaException>(() => Edicao(c).ExecuteAsync(Dados(usuario, id, "x"), default));
        missao.Status = StatusMissao.Ativa; missao.Campanha!.Estado = EstadoCampanha.Concluida; await c.SaveChangesAsync();
        await Assert.ThrowsAsync<CampanhaConcluidaException>(() => Edicao(c).ExecuteAsync(Dados(usuario, id, "x"), default));
        missao.Status = StatusMissao.Excluida; await c.SaveChangesAsync();
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Consulta(c).ExecuteAsync(new(usuario, usuario, id), default));
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Edicao(c).ExecuteAsync(Dados(usuario, id, "x"), default));
    }

    [FatoPostgres]
    public async Task ManutencaoMantemRegraEditadaELimitesSemRecriarDiasCancelados()
    {
        var (usuario, id) = await PrepararAsync(vinculada: true);
        await using (var c = banco.NovoContexto())
        {
            var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            await Edicao(c).ExecuteAsync(Dados(usuario, id, atual.Versao, TipoRecorrencia.Semanal, [Hoje.AddDays(2).DayOfWeek]), default);
        }
        await using var manutencao = banco.NovoContexto();
        var rotina = new EstenderJanelaDeOcorrenciasUseCase(new MissaoRepository(manutencao), new OcorrenciaRepository(manutencao), manutencao,
            NullLogger<EstenderJanelaDeOcorrenciasUseCase>.Instance);
        await rotina.ExecuteAsync(default);
        Assert.Equal(StatusOcorrencia.Excluida, (await manutencao.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje.AddDays(1))).Status);
        Assert.DoesNotContain(await manutencao.Ocorrencias.Where(o => o.MissaoId == id).ToListAsync(), o => o.DataPrevista > Hoje.AddDays(10));
    }

    private sealed class Intercalar(IUnitOfWork unidade, Func<Task> acao) : IUnitOfWork
    {
        public async Task SalvarAlteracoesAsync(CancellationToken ct) { await acao(); await unidade.SalvarAlteracoesAsync(ct); }
    }

    [FatoPostgres]
    public async Task EdicaoIntercaladaComConclusaoReverteRegraSemPerderCredito()
    {
        var (usuario, id) = await PrepararAsync();
        await using var edicao = banco.NovoContexto();
        var atual = await Consulta(edicao).ExecuteAsync(new(usuario, usuario, id), default);
        var unidade = new Intercalar(edicao, async () => { await using var outro = banco.NovoContexto(); await Conclusao(outro).ExecuteAsync(new(usuario, usuario, id), default); });
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Edicao(edicao, unidade).ExecuteAsync(Dados(usuario, id, atual.Versao), default));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(530, (await leitura.Usuarios.SingleAsync(u => u.Id == usuario)).SaldoPontos);
        Assert.Equal(2, await leitura.MovimentacoesPontos.CountAsync(m => m.UsuarioId == usuario));
        Assert.Equal("Ler uma aula", (await leitura.Missoes.SingleAsync(m => m.Id == id)).Titulo);
        Assert.Null((await leitura.Recorrencias.SingleAsync(r => r.MissaoId == id)).AlteracoesAPartirDe);
        Assert.Equal(6, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == id));
    }

    [FatoPostgres]
    public async Task EdicaoIntercaladaComExclusaoNaoReabreMissaoOuPlanejamento()
    {
        var (usuario, id) = await PrepararAsync();
        await using var edicao = banco.NovoContexto();
        var atual = await Consulta(edicao).ExecuteAsync(new(usuario, usuario, id), default);
        var unidade = new Intercalar(edicao, async () =>
        {
            await using var outro = banco.NovoContexto();
            await new ExcluirMissaoUseCase(new ExclusaoPlanejamentoRepository(outro), outro, new Relogio(Agora)).ExecuteAsync(new(usuario, usuario, id), default);
        });
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Edicao(edicao, unidade).ExecuteAsync(Dados(usuario, id, atual.Versao), default));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(StatusMissao.Excluida, (await leitura.Missoes.SingleAsync(m => m.Id == id)).Status);
        Assert.Null((await leitura.Recorrencias.SingleAsync(r => r.MissaoId == id)).AlteracoesAPartirDe);
        Assert.Equal(6, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == id));
    }
    [FatoPostgres]
    public async Task ExecucaoDeAmanhaUsaNovosPontosERepeticaoDeConclusaoAntigaUsaCreditoEfetivo()
    {
        var (usuario, id) = await PrepararAsync();
        await using (var c = banco.NovoContexto())
        {
            var hoje = await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje);
            hoje.Status = StatusOcorrencia.Concluida; hoje.ConcluidaEm = Agora; hoje.RealizadaEm = Agora;
            c.MovimentacoesPontos.Add(new() { UsuarioId = usuario, Ocorrencia = hoje, Tipo = TipoMovimentacaoPontos.GanhoMissao, Quantidade = 45 });
            await c.SaveChangesAsync();
            var atual = await Consulta(c).ExecuteAsync(new(usuario, usuario, id), default);
            await Edicao(c).ExecuteAsync(Dados(usuario, id, atual.Versao), default);
        }
        await using (var c = banco.NovoContexto())
        {
            var repeticao = await Conclusao(c).ExecuteAsync(new(usuario, usuario, id), default);
            Assert.Equal(45, repeticao.PontuacaoCreditada); Assert.Equal(500, repeticao.SaldoPontosAtual);
            Assert.Equal(2, await c.MovimentacoesPontos.CountAsync(m => m.UsuarioId == usuario));
            var amanha = await Conclusao(c, relogio: new Relogio(Agora.AddDays(1))).ExecuteAsync(new(usuario, usuario, id), default);
            Assert.Equal(60, amanha.PontuacaoCreditada); Assert.Equal(560, amanha.SaldoPontosAtual);
        }
    }

}
