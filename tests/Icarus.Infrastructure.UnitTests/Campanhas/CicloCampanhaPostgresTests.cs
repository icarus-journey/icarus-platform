using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.AtualizarCampanha;
using Icarus.Application.UseCases.ConcluirCampanha;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.CriarCampanha;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;
using Icarus.Application.UseCases.IniciarCampanhas;
using Icarus.Application.UseCases.ListarEpicos;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class CicloCampanhaPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private sealed class Relogio(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private static IniciarCampanhasUseCase Inicio(IcarusDbContext c, TimeProvider relogio)
        => new(new CicloCampanhaRepository(c), relogio);
    private static ConcluirCampanhaUseCase Conclusao(IcarusDbContext c, TimeProvider? relogio = null)
        => new(new CampanhaRepository(c), c, NullLogger<ConcluirCampanhaUseCase>.Instance,
            new CicloCampanhaRepository(c), relogio ?? TimeProvider.System);
    private static AtualizarCampanhaUseCase Edicao(IcarusDbContext c, TimeProvider relogio)
        => new(new CampanhaRepository(c), c, NullLogger<AtualizarCampanhaUseCase>.Instance, Inicio(c, relogio), relogio);

    private async Task<(Guid Usuario, long Campanha)> PrepararAsync(string fuso = "UTC", EstadoCampanha estado = EstadoCampanha.EmAndamento)
    {
        var usuario = await banco.CriarUsuarioAsync(500);
        await using var c = banco.NovoContexto();
        var pessoa = await c.Usuarios.SingleAsync(u => u.Id == usuario);
        pessoa.FusoHorario = fuso;
        var campanha = new Campanha
        {
            Epico = new Epico { UsuarioId = usuario, Titulo = "Aprender" },
            Titulo = "Curso",
            PrazoInicio = new(2026, 10, 25),
            PrazoFim = new(2027, 1, 1),
            Estado = estado,
            ValorMensurado = 12,
            ValorAtual = 2,
            Unidade = "módulos"
        };
        c.Campanhas.Add(campanha);
        await c.SaveChangesAsync();
        return (usuario, campanha.Id);
    }

    [FatoPostgres]
    public async Task IniciaNaDataLocalRecuperaDiasPassadosEIsolaTitularSemReabrirEncerradas()
    {
        var (brasileiro, campanhaBr) = await PrepararAsync("America/Sao_Paulo", EstadoCampanha.Planejada);
        var (outro, campanhaUtc) = await PrepararAsync("UTC", EstadoCampanha.Planejada);
        var (encerrado, campanhaEncerrada) = await PrepararAsync("UTC", EstadoCampanha.Concluida);
        var madrugada = new Relogio(new(2026, 10, 25, 1, 0, 0, TimeSpan.Zero));
        await using (var c = banco.NovoContexto())
        {
            // No Brasil ainda é dia 24; a consulta não antecipa o início nem mexe no outro titular.
            var lista = new ListarEpicosUseCase(new EpicoRepository(c), NullLogger<ListarEpicosUseCase>.Instance, Inicio(c, madrugada));
            Assert.Equal(EstadoCampanha.Planejada, (await lista.ExecuteAsync(new(brasileiro, brasileiro), default))[0].Campanhas[0].Estado);
            Assert.Equal(EstadoCampanha.Planejada, (await c.Campanhas.AsNoTracking().SingleAsync(x => x.Id == campanhaUtc)).Estado);
        }
        await using (var c = banco.NovoContexto()) Assert.Equal(1, await Inicio(c, madrugada).ExecuteAsync(outro, default));
        await using (var c = banco.NovoContexto())
        {
            // Recupera o início mesmo dias depois, inclusive se o épico já terminou (sem cascata).
            (await c.Epicos.SingleAsync(e => e.UsuarioId == brasileiro)).Estado = EstadoEpico.Concluido;
            await c.SaveChangesAsync();
            Assert.Equal(1, await Inicio(c, new Relogio(new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero))).ExecuteAsync(null, default));
            Assert.Equal(0, await Inicio(c, madrugada).ExecuteAsync(null, default));
        }
        await using var leitura = banco.NovoContexto();
        Assert.Equal(EstadoCampanha.EmAndamento, (await leitura.Campanhas.SingleAsync(c => c.Id == campanhaBr)).Estado);
        Assert.Equal(EstadoCampanha.Concluida, (await leitura.Campanhas.SingleAsync(c => c.Id == campanhaEncerrada)).Estado);
        Assert.Equal(500, (await leitura.Usuarios.SingleAsync(u => u.Id == brasileiro)).SaldoPontos);
        Assert.Empty(await leitura.MedicoesCampanhas.Where(m => m.CampanhaId == campanhaBr).ToListAsync());
        Assert.Empty(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == brasileiro).ToListAsync());
    }

    [FatoPostgres]
    public async Task PlanejadaAceitaConfiguracaoAteADataInicialEDepoisSomenteMedicoes()
    {
        var (usuario, id) = await PrepararAsync("America/Sao_Paulo", EstadoCampanha.Planejada);
        var madrugada = new Relogio(new(2026, 10, 25, 1, 0, 0, TimeSpan.Zero));
        var dados = new AtualizarCampanhaCommand(usuario, usuario, id, "Curso completo", "Nova descrição", Dificuldade.Dificil,
            new(2026, 10, 25), new(2027, 2, 1), 16, "aulas");
        await using (var c = banco.NovoContexto())
        {
            var editada = await Edicao(c, madrugada).ExecuteAsync(dados, default);
            Assert.Equal(EstadoCampanha.Planejada, editada.Estado);
            Assert.Equal(16m, editada.ValorMensurado);
            Assert.Equal(2m, editada.ValorAtual);
        }
        var diaInicial = new Relogio(new(2026, 10, 25, 3, 0, 0, TimeSpan.Zero));
        await using (var c = banco.NovoContexto())
            await Assert.ThrowsAsync<ConfiguracaoCampanhaBloqueadaException>(() => Edicao(c, diaInicial).ExecuteAsync(dados with { Titulo = "Não pode", ValorMensurado = 30 }, default));
        await using (var c = banco.NovoContexto())
        {
            var campanha = await c.Campanhas.SingleAsync(x => x.Id == id);
            Assert.Equal(EstadoCampanha.EmAndamento, campanha.Estado);
            Assert.Equal("Curso completo", campanha.Titulo);
            Assert.Equal(16m, campanha.ValorMensurado);
            var registro = new Icarus.Application.UseCases.RegistrarProgressoCampanha.RegistrarProgressoCampanhaUseCase(
                new CampanhaRepository(c), new MedicaoCampanhaRepository(c), new UsuarioRepository(c), c, diaInicial,
                NullLogger<Icarus.Application.UseCases.RegistrarProgressoCampanha.RegistrarProgressoCampanhaUseCase>.Instance, Inicio(c, diaInicial));
            var resultado = await registro.ExecuteAsync(new(usuario, usuario, id, 3,
                Icarus.Application.Common.ProgressoCampanhaResult.VersaoDaCampanha(campanha), Guid.NewGuid()), default);
            Assert.Equal(3m, resultado.ValorAtual);
            Assert.Single(resultado.Medicoes);
        }
    }

    [FatoPostgres]
    public async Task CriacaoAssumeHojeLocalEEdicaoPodeAnteciparInicioDePlanejada()
    {
        var (usuario, id) = await PrepararAsync("America/Sao_Paulo", EstadoCampanha.Planejada);
        var relogio = new Relogio(new(2026, 10, 25, 1, 0, 0, TimeSpan.Zero));
        await using var c = banco.NovoContexto();
        var epico = (await c.Campanhas.AsNoTracking().SingleAsync(x => x.Id == id)).EpicoId;
        var criacao = new CriarCampanhaUseCase(new EpicoRepository(c), new CampanhaRepository(c), c,
            NullLogger<CriarCampanhaUseCase>.Instance, relogio);
        var criada = await criacao.ExecuteAsync(new(usuario, usuario, epico, "Hoje", null, Dificuldade.Facil, null, new(2027, 1, 1), 12, "aulas"), default);
        Assert.Equal(new DateOnly(2026, 10, 24), criada.PrazoInicio);
        Assert.Equal(EstadoCampanha.EmAndamento, criada.Estado);
        var editada = await Edicao(c, relogio).ExecuteAsync(new(usuario, usuario, id, "Antecipada", null, Dificuldade.Media,
            new(2026, 10, 24), new(2027, 1, 1), 12, "módulos"), default);
        Assert.Equal(EstadoCampanha.EmAndamento, editada.Estado);
    }

    [FatoPostgres]
    public async Task ConclusaoPreservaHistoricoPontosEMissoesAvulsasOuDeOutraCampanha()
    {
        var (usuario, id) = await PrepararAsync();
        var (outro, outraCampanha) = await PrepararAsync();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        long missaoId;
        await using (var c = banco.NovoContexto())
        {
            var missao = new Missao
            {
                UsuarioId = usuario,
                CampanhaId = id,
                Titulo = "Praticar",
                Pontuacao = 30,
                Recorrencia = new Recorrencia { Tipo = "DIARIA", DataInicio = hoje.AddDays(-2) }
            };
            missao.Ocorrencias.Add(new Ocorrencia { DataPrevista = hoje.AddDays(-2) });
            missao.Ocorrencias.Add(new Ocorrencia { DataPrevista = hoje.AddDays(-1), Status = StatusOcorrencia.Atrasada });
            missao.Ocorrencias.Add(new Ocorrencia { DataPrevista = hoje });
            missao.Ocorrencias.Add(new Ocorrencia { DataPrevista = hoje.AddDays(1) });
            missao.Ocorrencias.Add(new Ocorrencia { DataPrevista = hoje.AddDays(2), Status = StatusOcorrencia.Concluida, ConcluidaEm = DateTimeOffset.UtcNow });
            c.Missoes.Add(missao);
            c.Missoes.Add(new Missao { UsuarioId = outro, CampanhaId = outraCampanha, Titulo = "Outra", Recorrencia = new() { Tipo = "DIARIA", DataInicio = hoje }, Ocorrencias = [new() { DataPrevista = hoje.AddDays(1) }] });
            c.Missoes.Add(new Missao { UsuarioId = usuario, Titulo = "Avulsa", Ocorrencias = [new() { DataPrevista = hoje.AddDays(1) }] });
            c.MedicoesCampanhas.Add(new MedicaoCampanha { CampanhaId = id, ValorAtual = 2, ValorMensurado = 12, Unidade = "módulos", OperacaoId = Guid.NewGuid(), RegistradaEm = DateTimeOffset.UtcNow });
            await c.SaveChangesAsync();
            missaoId = missao.Id;
            var concluir = new ConcluirMissaoUseCase(new MissaoRepository(c), new OcorrenciaRepository(c), new MovimentacaoPontosRepository(c),
                new CampanhaRepository(c), new UsuarioRepository(c), c, NullLogger<ConcluirMissaoUseCase>.Instance);
            await concluir.ExecuteAsync(new(usuario, usuario, missaoId), default);
        }
        await using (var c = banco.NovoContexto())
        {
            await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => Conclusao(c).ExecuteAsync(new(outro, outro, id), default));
            await Conclusao(c).ExecuteAsync(new(usuario, usuario, id), default);
            await Conclusao(c).ExecuteAsync(new(usuario, usuario, id), default);
        }
        await using var leitura = banco.NovoContexto();
        var finais = await leitura.Ocorrencias.Where(o => o.MissaoId == missaoId).OrderBy(o => o.DataPrevista).ToListAsync();
        Assert.Equal(new[] { StatusOcorrencia.Pendente, StatusOcorrencia.Atrasada, StatusOcorrencia.Concluida,
            StatusOcorrencia.Excluida, StatusOcorrencia.Concluida }, finais.Select(o => o.Status));
        Assert.NotNull(finais[2].RealizadaEm);
        Assert.Equal(StatusMissao.Inativa, (await leitura.Missoes.SingleAsync(m => m.Id == missaoId)).Status);
        Assert.Equal(530, (await leitura.Usuarios.SingleAsync(u => u.Id == usuario)).SaldoPontos);
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == usuario).ToListAsync());
        Assert.Single(await leitura.MedicoesCampanhas.Where(m => m.CampanhaId == id).ToListAsync());
        Assert.All(await leitura.Ocorrencias.Where(o => o.MissaoId != missaoId).ToListAsync(), o => Assert.Equal(StatusOcorrencia.Pendente, o.Status));
        var manutencao = new EstenderJanelaDeOcorrenciasUseCase(new MissaoRepository(leitura), new OcorrenciaRepository(leitura), leitura,
            NullLogger<EstenderJanelaDeOcorrenciasUseCase>.Instance);
        await manutencao.ExecuteAsync(default);
        Assert.Equal(5, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == missaoId));
    }

    // Intercala uma conclusão real entre a leitura da campanha e a gravação de novo planejamento.
    private sealed class OcorrenciaIntercalada(IOcorrenciaRepository origem, Func<Task> intercalar) : IOcorrenciaRepository
    {
        private bool _executou;
        public async Task AdicionarAsync(Ocorrencia ocorrencia, CancellationToken ct)
        {
            if (!_executou) { _executou = true; await intercalar(); }
            await origem.AdicionarAsync(ocorrencia, ct);
        }
        public Task<Ocorrencia?> BuscarPorMissaoEDataAsync(long id, DateOnly data, CancellationToken ct) => origem.BuscarPorMissaoEDataAsync(id, data, ct);
        public Task<Ocorrencia?> BuscarParaConcluirAsync(long id, DateOnly hoje, CancellationToken ct) => origem.BuscarParaConcluirAsync(id, hoje, ct);
        public Task<List<DateOnly>> ListarDatasExistentesAsync(long id, DateOnly de, DateOnly ate, CancellationToken ct) => origem.ListarDatasExistentesAsync(id, de, ate, ct);
        public Task<List<Ocorrencia>> ListarPendentesNaoVencidasAsync(long id, DateOnly hoje, CancellationToken ct) => origem.ListarPendentesNaoVencidasAsync(id, hoje, ct);
    }

    [FatoPostgres]
    public async Task ManutencaoQueLeuCampanhaAntesDaConclusaoNaoRecriaPlanejamento()
    {
        var (usuario, id) = await PrepararAsync();
        await using (var c = banco.NovoContexto())
        {
            c.Missoes.Add(new Missao { UsuarioId = usuario, CampanhaId = id, Titulo = "Recorrente", Recorrencia = new() { Tipo = "DIARIA", DataInicio = DateOnly.FromDateTime(DateTime.UtcNow) } });
            await c.SaveChangesAsync();
        }
        await using var concorrente = banco.NovoContexto();
        var ocorrencias = new OcorrenciaIntercalada(new OcorrenciaRepository(concorrente), async () =>
        {
            await using var finalizacao = banco.NovoContexto();
            await Conclusao(finalizacao).ExecuteAsync(new(usuario, usuario, id), default);
        });
        var manutencao = new EstenderJanelaDeOcorrenciasUseCase(new MissaoRepository(concorrente), ocorrencias, concorrente,
            NullLogger<EstenderJanelaDeOcorrenciasUseCase>.Instance);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => manutencao.ExecuteAsync(default));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(EstadoCampanha.Concluida, (await leitura.Campanhas.SingleAsync(c => c.Id == id)).Estado);
        Assert.Empty(await leitura.Ocorrencias.Where(o => o.Missao.CampanhaId == id).ToListAsync());
    }

    [FatoPostgres]
    public async Task CriacaoQueLeuCampanhaAntesDaConclusaoNaoDeixaMissaoOuOcorrenciaOrfa()
    {
        var (usuario, id) = await PrepararAsync();
        await using var concorrente = banco.NovoContexto();
        var ocorrencias = new OcorrenciaIntercalada(new OcorrenciaRepository(concorrente), async () =>
        {
            await using var finalizacao = banco.NovoContexto();
            await Conclusao(finalizacao).ExecuteAsync(new(usuario, usuario, id), default);
        });
        var criar = new CriarMissaoUseCase(new CampanhaRepository(concorrente), new MissaoRepository(concorrente),
            new RecorrenciaRepository(concorrente), ocorrencias, concorrente, NullLogger<CriarMissaoUseCase>.Instance);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => criar.ExecuteAsync(new(usuario, usuario, id, "Tardia", null, null,
            Dificuldade.Facil, null, null, new(TipoRecorrencia.Diaria, null, null)), default));
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.Missoes.Where(m => m.CampanhaId == id).ToListAsync());
    }
}
