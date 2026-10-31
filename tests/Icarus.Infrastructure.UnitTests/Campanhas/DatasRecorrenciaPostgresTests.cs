using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.ConsultarMissao;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;
using Icarus.Application.UseCases.InativarMissao;
using Icarus.Application.UseCases.IniciarCampanhas;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class DatasRecorrenciaPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 31, 1, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoje = new(2026, 10, 30);
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private static IniciarCampanhasUseCase Inicio(IcarusDbContext c) => new(new CicloCampanhaRepository(c), new Relogio());
    private static ConsultarMissaoUseCase Consulta(IcarusDbContext c) => new(new PlanejamentoMissaoRepository(c), c, new Relogio(), Inicio(c));
    private static ConcluirMissaoUseCase Conclusao(IcarusDbContext c, IUnitOfWork? unidade = null) => new(new MissaoRepository(c),
        new OcorrenciaRepository(c), new MovimentacaoPontosRepository(c), new CampanhaRepository(c), new UsuarioRepository(c),
        unidade ?? c, new TransacaoConclusaoMissao(c), NullLogger<ConcluirMissaoUseCase>.Instance, new Relogio(), Inicio(c));
    private static EstenderJanelaDeOcorrenciasUseCase Manutencao(IcarusDbContext c) => new(new MissaoRepository(c),
        new OcorrenciaRepository(c), c, NullLogger<EstenderJanelaDeOcorrenciasUseCase>.Instance, new Relogio());
    private static CriarMissaoUseCase Criacao(IcarusDbContext c) => new(new CampanhaRepository(c), new MissaoRepository(c),
        new RecorrenciaRepository(c), new OcorrenciaRepository(c), c, NullLogger<CriarMissaoUseCase>.Instance,
        new Relogio(), new UsuarioRepository(c));
    private static CriarMissaoCommand Comando(Guid u, long? campanha = null, DateOnly? fim = null, bool recorrente = true, DateOnly? data = null)
        => new(u, u, campanha, "Praticar uma aula", null, null, Dificuldade.Facil, data, null,
            recorrente ? new(TipoRecorrencia.Diaria, null, fim) : null);

    private async Task<Guid> Usuario(string fuso = "America/Sao_Paulo")
    {
        var u = await banco.CriarUsuarioAsync(100);
        await using var c = banco.NovoContexto();
        (await c.Usuarios.SingleAsync(x => x.Id == u)).FusoHorario = fuso;
        await c.SaveChangesAsync(); return u;
    }
    private async Task<long> Campanha(Guid u, DateOnly inicio, DateOnly fim, EstadoCampanha estado = EstadoCampanha.Planejada)
    {
        await using var c = banco.NovoContexto();
        var campanha = new Campanha
        {
            Epico = new() { UsuarioId = u, Titulo = "Estudos" },
            Titulo = "Curso",
            PrazoInicio = inicio,
            PrazoFim = fim,
            Estado = estado,
            ValorMensurado = 10,
            ValorAtual = 2,
            Unidade = "módulos"
        };
        c.Campanhas.Add(campanha); await c.SaveChangesAsync(); return campanha.Id;
    }
    private async Task<long> Legada(Guid u, DateOnly[] datas, bool recorrente = true, long? campanha = null, DateOnly? fim = null)
    {
        await using var c = banco.NovoContexto();
        var m = new Missao
        {
            UsuarioId = u,
            CampanhaId = campanha,
            Titulo = "Planejamento anterior",
            Pontuacao = 30,
            Recorrencia = recorrente ? new() { Tipo = "DIARIA", DataInicio = Hoje.AddDays(-30), DataFim = fim, Regra = "{}" } : null,
            Ocorrencias = datas.Select(d => new Ocorrencia { DataPrevista = d }).ToList()
        };
        c.Missoes.Add(m); await c.SaveChangesAsync(); return m.Id;
    }

    [FatoPostgres]
    public async Task CriacaoUsaFusoDoTitularIncluiFimLocalERestringeJanelaA31Dias()
    {
        foreach (var (fuso, dia) in new[] { ("America/Sao_Paulo", Hoje), ("UTC", Hoje.AddDays(1)), ("Asia/Tokyo", Hoje.AddDays(1)), ("legado/invalido", Hoje.AddDays(1)) })
        {
            var u = await Usuario(fuso); await using var c = banco.NovoContexto();
            var diaria = await Criacao(c).ExecuteAsync(Comando(u), default);
            var datas = await c.Ocorrencias.Where(o => o.MissaoId == diaria.Id).OrderBy(o => o.DataPrevista).Select(o => o.DataPrevista).ToListAsync();
            Assert.Equal(31, datas.Count); Assert.Equal(dia, datas[0]); Assert.Equal(dia.AddDays(30), datas[^1]);
            var fimHoje = await Criacao(c).ExecuteAsync(Comando(u, fim: dia), default);
            Assert.Equal(dia, (await c.Ocorrencias.SingleAsync(o => o.MissaoId == fimHoje.Id)).DataPrevista);
        }
    }

    [FatoPostgres]
    public async Task CampanhaFuturaLimitaInicioEFimEBloqueiaConclusaoSemCriarExecucaoHoje()
    {
        var u = await Usuario(); var campanha = await Campanha(u, Hoje.AddDays(2), Hoje.AddDays(5));
        await using var c = banco.NovoContexto();
        var m = await Criacao(c).ExecuteAsync(Comando(u, campanha), default);
        var datas = await c.Ocorrencias.Where(o => o.MissaoId == m.Id).OrderBy(o => o.DataPrevista).Select(o => o.DataPrevista).ToListAsync();
        Assert.Equal(Enumerable.Range(2, 4).Select(Hoje.AddDays), datas);
        var leitura = await Consulta(c).ExecuteAsync(new(u, u, m.Id), default);
        Assert.Equal("1.3.0", leitura.VersaoContrato); Assert.False(leitura.Execucao.PodeConcluir);
        Assert.Equal("campanha_nao_iniciada", leitura.Execucao.CodigoBloqueio);
        var erro = await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(new(u, u, m.Id), default));
        Assert.Equal("campanha_nao_iniciada", erro.Codigo);
        Assert.Equal(4, await c.Ocorrencias.CountAsync(o => o.MissaoId == m.Id));
        Assert.Empty(await c.MovimentacoesPontos.Where(p => p.UsuarioId == u).ToListAsync());
    }

    [FatoPostgres]
    public async Task CriacaoRecusaFimAnteriorAoInicioEUnicaForaDaCampanhaSemPersistir()
    {
        var u = await Usuario(); var campanha = await Campanha(u, Hoje.AddDays(2), Hoje.AddDays(5));
        await using var c = banco.NovoContexto();
        await Assert.ThrowsAsync<EdicaoMissaoException>(() => Criacao(c).ExecuteAsync(Comando(u, campanha, Hoje.AddDays(1)), default));
        await Assert.ThrowsAsync<EdicaoMissaoException>(() => Criacao(c).ExecuteAsync(Comando(u, campanha, recorrente: false, data: Hoje), default));
        Assert.Empty(await c.Missoes.Where(m => m.UsuarioId == u).ToListAsync());
    }

    [FatoPostgres]
    public async Task ConsultaIniciaCampanhaNoDiaLocalEConclusaoNaoTransformaPontosEmModulos()
    {
        var u = await Usuario(); var campanha = await Campanha(u, Hoje, Hoje.AddDays(5));
        long id; await using (var c = banco.NovoContexto()) id = (await Criacao(c).ExecuteAsync(Comando(u, campanha), default)).Id;
        await using var leitura = banco.NovoContexto();
        var dados = await Consulta(leitura).ExecuteAsync(new(u, u, id), default);
        Assert.True(dados.Execucao.PodeConcluir); Assert.Equal(Hoje, dados.Execucao.Hoje); Assert.Equal("America/Sao_Paulo", dados.Execucao.FusoHorario);
        var resultado = await Conclusao(leitura).ExecuteAsync(new(u, u, id, dados.Execucao.OcorrenciaId), default);
        Assert.False(resultado.ConcluidaComAtraso); Assert.Equal(130, resultado.SaldoPontosAtual);
        Assert.Equal(EstadoCampanha.EmAndamento, (await leitura.Campanhas.SingleAsync(c => c.Id == campanha)).Estado);
        Assert.Equal(2, (await leitura.Campanhas.SingleAsync(c => c.Id == campanha)).ValorAtual);
    }

    [FatoPostgres]
    public async Task ConclusaoTambemRecuperaInicioAutomaticoSemConsultaAnterior()
    {
        var u = await Usuario(); var campanha = await Campanha(u, Hoje, Hoje.AddDays(5));
        long id; await using (var c = banco.NovoContexto()) id = (await Criacao(c).ExecuteAsync(Comando(u, campanha), default)).Id;
        await using var conclusao = banco.NovoContexto();
        Assert.Equal(130, (await Conclusao(conclusao).ExecuteAsync(new(u, u, id), default)).SaldoPontosAtual);
    }

    [FatoPostgres]
    public async Task FuturoOuCanceladaNaoConcluiENaoMaterializaDefensivamente()
    {
        var u = await Usuario(); var id = await Legada(u, [Hoje.AddDays(1)], recorrente: false);
        await using var c = banco.NovoContexto();
        var futura = await c.Ocorrencias.SingleAsync(o => o.MissaoId == id);
        foreach (var ocorrencia in new long?[] { null, futura.Id })
            await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(new(u, u, id, ocorrencia), default));
        futura.DataPrevista = Hoje; futura.Status = StatusOcorrencia.Excluida; await c.SaveChangesAsync();
        await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(new(u, u, id), default));
        Assert.Single(await c.Ocorrencias.Where(o => o.MissaoId == id).ToListAsync());
        Assert.Empty(await c.MovimentacoesPontos.Where(p => p.UsuarioId == u).ToListAsync());
        Assert.Equal(100, (await c.Usuarios.SingleAsync(x => x.Id == u)).SaldoPontos);
    }

    [FatoPostgres]
    public async Task SelecaoPriorizaHojeERetryPorIdPreservaCreditoSemConcluirOutroAtraso()
    {
        var u = await Usuario(); var id = await Legada(u, [Hoje.AddDays(-2), Hoje, Hoje.AddDays(1)]);
        long hojeId;
        await using (var c = banco.NovoContexto())
        {
            var dados = await Consulta(c).ExecuteAsync(new(u, u, id), default);
            hojeId = dados.Execucao.OcorrenciaId!.Value; Assert.Equal(Hoje, dados.Execucao.DataPrevista);
            Assert.False((await Conclusao(c).ExecuteAsync(new(u, u, id, hojeId), default)).Repetida);
        }
        await using var repeticao = banco.NovoContexto();
        var resultado = await Conclusao(repeticao).ExecuteAsync(new(u, u, id, hojeId), default);
        Assert.True(resultado.Repetida); Assert.Equal(30, resultado.PontuacaoCreditada); Assert.Equal(130, resultado.SaldoPontosAtual);
        Assert.Single(await repeticao.MovimentacoesPontos.Where(p => p.UsuarioId == u).ToListAsync());
        Assert.Equal(StatusOcorrencia.Pendente, (await repeticao.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje.AddDays(-2))).Status);
    }

    [FatoPostgres]
    public async Task SemHojeConcluiAtrasoMaisAntigoSemEscolherFuturo()
    {
        var u = await Usuario(); var id = await Legada(u, [Hoje.AddDays(-3), Hoje.AddDays(-1), Hoje.AddDays(1)]);
        await using var c = banco.NovoContexto();
        var dados = await Consulta(c).ExecuteAsync(new(u, u, id), default);
        Assert.Equal(Hoje.AddDays(-3), dados.Execucao.DataPrevista); Assert.True(dados.Execucao.Atrasada);
        Assert.True((await Conclusao(c).ExecuteAsync(new(u, u, id, dados.Execucao.OcorrenciaId), default)).ConcluidaComAtraso);
    }

    [FatoPostgres]
    public async Task PararNaViradaPreservaOntemECancelaHojeEFuturoInclusiveStatusAtrasada()
    {
        var u = await Usuario(); var id = await Legada(u, [Hoje.AddDays(-1), Hoje, Hoje.AddDays(1)]);
        await using var c = banco.NovoContexto();
        (await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje)).Status = StatusOcorrencia.Atrasada;
        await c.SaveChangesAsync();
        var caso = new InativarMissaoUseCase(new MissaoRepository(c), new OcorrenciaRepository(c), c, NullLogger<InativarMissaoUseCase>.Instance, new Relogio());
        Assert.Equal(2, (await caso.ExecuteAsync(new(u, u, id), default)).OcorrenciasCanceladas);
        Assert.Equal(StatusOcorrencia.Pendente, (await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje.AddDays(-1))).Status);
        Assert.Equal(0, (await caso.ExecuteAsync(new(u, u, id), default)).OcorrenciasCanceladas);
    }

    [FatoPostgres]
    public async Task FimDaCampanhaInativaNaConsultaENaConclusaoSemEsperarRotina()
    {
        var u = await Usuario(); var campanha = await Campanha(u, Hoje.AddDays(-20), Hoje.AddDays(-1), EstadoCampanha.EmAndamento);
        foreach (var consultar in new[] { false, true })
        {
            var id = await Legada(u, [Hoje.AddDays(-2), Hoje.AddDays(1)], campanha: campanha);
            await using (var c = banco.NovoContexto())
            {
                if (consultar) Assert.Equal(StatusMissao.Inativa, (await Consulta(c).ExecuteAsync(new(u, u, id), default)).Status);
                await Assert.ThrowsAsync<MissaoInativaException>(() => Conclusao(c).ExecuteAsync(new(u, u, id), default));
            }
            await using var leitura = banco.NovoContexto();
            Assert.Equal(StatusMissao.Inativa, (await leitura.Missoes.SingleAsync(m => m.Id == id)).Status);
            Assert.Equal(StatusOcorrencia.Pendente, (await leitura.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista < Hoje)).Status);
            Assert.Equal(StatusOcorrencia.Excluida, (await leitura.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista > Hoje)).Status);
        }
    }

    [FatoPostgres]
    public async Task ManutencaoLimitaLegadoPreservaConcluidasECancelamentoDeHojeENaoDuplicaJanela()
    {
        var u = await Usuario(); var campanha = await Campanha(u, Hoje.AddDays(2), Hoje.AddDays(5));
        var id = await Legada(u, [Hoje.AddDays(-1), Hoje, Hoje.AddDays(1), Hoje.AddDays(3), Hoje.AddDays(6), Hoje.AddDays(7)], campanha: campanha);
        long reutilizada;
        await using (var c = banco.NovoContexto())
        {
            var concluida = await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje.AddDays(7));
            concluida.Status = StatusOcorrencia.Concluida; concluida.ConcluidaEm = Agora;
            var cancelada = await c.Ocorrencias.SingleAsync(o => o.MissaoId == id && o.DataPrevista == Hoje.AddDays(3));
            cancelada.Status = StatusOcorrencia.Excluida; reutilizada = cancelada.Id;
            await c.SaveChangesAsync();
            await Manutencao(c).ExecuteAsync(default); await Manutencao(c).ExecuteAsync(default);
        }
        await using var leitura = banco.NovoContexto();
        var linhas = await leitura.Ocorrencias.Where(o => o.MissaoId == id).ToListAsync();
        Assert.Equal(linhas.Count, linhas.Select(o => o.DataPrevista).Distinct().Count());
        Assert.Equal(StatusOcorrencia.Pendente, linhas.Single(o => o.DataPrevista < Hoje).Status);
        Assert.Equal(StatusOcorrencia.Excluida, linhas.Single(o => o.DataPrevista == Hoje).Status);
        Assert.Equal(StatusOcorrencia.Excluida, linhas.Single(o => o.DataPrevista == Hoje.AddDays(6)).Status);
        Assert.Equal(StatusOcorrencia.Concluida, linhas.Single(o => o.DataPrevista == Hoje.AddDays(7)).Status);
        Assert.Equal(StatusOcorrencia.Pendente, linhas.Single(o => o.Id == reutilizada).Status);
        Assert.All(linhas.Where(o => o.Status == StatusOcorrencia.Pendente && o.DataPrevista >= Hoje), o => Assert.InRange(o.DataPrevista, Hoje.AddDays(2), Hoje.AddDays(5)));
    }

    [FatoPostgres]
    public async Task ManutencaoEncerraFimDaRegraSemReabrirMissoesParadas()
    {
        var u = await Usuario(); var id = await Legada(u, [Hoje.AddDays(-2), Hoje.AddDays(1)], fim: Hoje.AddDays(-1));
        await using var c = banco.NovoContexto();
        var resultado = await Manutencao(c).ExecuteAsync(default); Assert.True(resultado.MissoesInativadasPorFimDaRecorrencia > 0);
        Assert.Equal(StatusMissao.Inativa, (await c.Missoes.SingleAsync(m => m.Id == id)).Status);
        await Manutencao(c).ExecuteAsync(default);
        Assert.Equal(2, await c.Ocorrencias.CountAsync(o => o.MissaoId == id));
    }

    [FatoPostgres]
    public async Task IsolamentoDaConsultaEDaOcorrenciaNaoExpoeOutroTitular()
    {
        var u = await Usuario(); var outro = await Usuario();
        var id = await Legada(u, [Hoje]); var outroId = await Legada(outro, [Hoje]);
        await using var c = banco.NovoContexto();
        var ocorrenciaOutro = (await c.Ocorrencias.SingleAsync(o => o.MissaoId == outroId)).Id;
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Consulta(c).ExecuteAsync(new(outro, u, id), default));
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Consulta(c).ExecuteAsync(new(outro, outro, id), default));
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Conclusao(c).ExecuteAsync(new(u, u, id, ocorrenciaOutro), default));
        Assert.Empty(await c.MovimentacoesPontos.Where(p => p.UsuarioId == u || p.UsuarioId == outro).ToListAsync());
    }
    [FatoPostgres]
    public async Task UnicaLegadaJaConcluidaSemOcorrenciaNaoFabricaOutroCredito()
    {
        var u = await Usuario(); var id = await Legada(u, [], recorrente: false);
        await using var c = banco.NovoContexto();
        (await c.Missoes.SingleAsync(m => m.Id == id)).ConcluidaEm = Agora;
        await c.SaveChangesAsync();
        var dados = await Consulta(c).ExecuteAsync(new(u, u, id), default);
        Assert.True(dados.Execucao.Concluida); Assert.False(dados.Execucao.PodeConcluir);
        await Assert.ThrowsAsync<EdicaoMissaoException>(() => Conclusao(c).ExecuteAsync(new(u, u, id), default));
        Assert.Empty(await c.Ocorrencias.Where(o => o.MissaoId == id).ToListAsync());
        Assert.Empty(await c.MovimentacoesPontos.Where(p => p.UsuarioId == u).ToListAsync());
        Assert.Equal(100, (await c.Usuarios.SingleAsync(x => x.Id == u)).SaldoPontos);
    }

}
