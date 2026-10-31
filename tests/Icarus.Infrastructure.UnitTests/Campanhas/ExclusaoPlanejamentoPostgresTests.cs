using Icarus.Application.Common.Analise;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.CriarCampanha;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;
using Icarus.Application.UseCases.ExcluirCampanha;
using Icarus.Application.UseCases.ExcluirEpico;
using Icarus.Application.UseCases.ExcluirMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Campanhas;

public sealed class ExclusaoPlanejamentoPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 25, 1, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoje = new(2026, 10, 24); // Ainda é dia 24 em São Paulo.
    private sealed class Relogio : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private static ExcluirEpicoUseCase Epico(IcarusDbContext c, IUnitOfWork? unidade = null)
        => new(new ExclusaoPlanejamentoRepository(c), unidade ?? c, new Relogio());
    private static ExcluirCampanhaUseCase Campanha(IcarusDbContext c, IUnitOfWork? unidade = null)
        => new(new ExclusaoPlanejamentoRepository(c), unidade ?? c, new Relogio());
    private static ExcluirMissaoUseCase Missao(IcarusDbContext c, IUnitOfWork? unidade = null)
        => new(new ExclusaoPlanejamentoRepository(c), unidade ?? c, new Relogio());
    private static ConcluirMissaoUseCase Conclusao(IcarusDbContext c, IUnitOfWork? unidade = null)
        => new(new MissaoRepository(c), new OcorrenciaRepository(c), new MovimentacaoPontosRepository(c),
            new CampanhaRepository(c), new UsuarioRepository(c), unidade ?? c, NullLogger<ConcluirMissaoUseCase>.Instance);

    private async Task<(Guid Usuario, long Epico, long Campanha, long Recorrente, long Concluida, long Pendente)> PrepararAsync()
    {
        var usuario = await banco.CriarUsuarioAsync(500);
        await using var c = banco.NovoContexto();
        (await c.Usuarios.SingleAsync(u => u.Id == usuario)).FusoHorario = "America/Sao_Paulo";
        var campanha = new Campanha
        {
            Epico = new Epico { UsuarioId = usuario, Titulo = "Estudar" },
            Titulo = "Curso",
            Estado = EstadoCampanha.EmAndamento,
            ValorMensurado = 12,
            ValorAtual = 2,
            Unidade = "módulos"
        };
        Missao Nova(string titulo) => new() { UsuarioId = usuario, Campanha = campanha, Titulo = titulo, Pontuacao = 30 };
        var recorrente = Nova("Praticar");
        recorrente.Recorrencia = new() { Tipo = "DIARIA", DataInicio = Hoje.AddDays(-2) };
        recorrente.Ocorrencias = [
            new() { DataPrevista = Hoje.AddDays(-2), Status = StatusOcorrencia.Atrasada },
            new() { DataPrevista = Hoje.AddDays(-1), Status = StatusOcorrencia.Pendente },
            new() { DataPrevista = Hoje, Status = StatusOcorrencia.Pendente },
            new() { DataPrevista = Hoje.AddDays(1), Status = StatusOcorrencia.Pendente },
            new() { DataPrevista = Hoje.AddDays(2), Status = StatusOcorrencia.Concluida, ConcluidaEm = Agora, RealizadaEm = Agora }];
        var concluida = Nova("Concluída"); concluida.ConcluidaEm = Agora;
        concluida.Ocorrencias = [new() { DataPrevista = Hoje, Status = StatusOcorrencia.ConcluidaComAtraso, ConcluidaEm = Agora, RealizadaEm = Agora }];
        var pendente = Nova("Uma vez"); pendente.Ocorrencias = [new() { DataPrevista = Hoje.AddDays(-5), Status = StatusOcorrencia.Atrasada }];
        c.Missoes.AddRange(recorrente, concluida, pendente);
        c.Missoes.Add(new() { UsuarioId = usuario, Titulo = "Avulsa", Ocorrencias = [new() { DataPrevista = Hoje }] });
        c.MedicoesCampanhas.Add(new() { Campanha = campanha, ValorAtual = 2, ValorMensurado = 12, Unidade = "módulos", OperacaoId = Guid.NewGuid(), RegistradaEm = Agora });
        c.MovimentacoesPontos.Add(new() { UsuarioId = usuario, Ocorrencia = concluida.Ocorrencias.Single(), Quantidade = 30, Tipo = TipoMovimentacaoPontos.GanhoMissao });
        c.MissoesIA.Add(new() { UsuarioId = usuario, Campanha = campanha, Titulo = "Rascunho", Status = StatusMissao.Rascunho });
        c.MissoesIA.Add(new() { UsuarioId = usuario, Campanha = campanha, Missao = concluida, Titulo = "Sugestão confirmada", Status = StatusMissao.Ativa });
        await c.SaveChangesAsync();
        return (usuario, campanha.EpicoId, campanha.Id, recorrente.Id, concluida.Id, pendente.Id);
    }

    [FatoPostgres]
    public async Task CampanhaExcluiSomentePlanejamentoElegivelPreservaHistoricoEMedicoesEExpurgaRascunho()
    {
        var p = await PrepararAsync();
        await using (var c = banco.NovoContexto())
        {
            await Campanha(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Campanha), default);
            await Campanha(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Campanha), default);
        }
        await using var leitura = banco.NovoContexto();
        Assert.Equal(EstadoCampanha.Excluida, (await leitura.Campanhas.SingleAsync(x => x.Id == p.Campanha)).Estado);
        Assert.Equal(EstadoEpico.EmAndamento, (await leitura.Epicos.SingleAsync(x => x.Id == p.Epico)).Estado);
        Assert.Equal(StatusMissao.Excluida, (await leitura.Missoes.SingleAsync(x => x.Id == p.Recorrente)).Status);
        Assert.Equal(StatusMissao.Excluida, (await leitura.Missoes.SingleAsync(x => x.Id == p.Pendente)).Status);
        Assert.Equal(StatusMissao.Ativa, (await leitura.Missoes.SingleAsync(x => x.Id == p.Concluida)).Status);
        Assert.Equal(Agora, (await leitura.Missoes.SingleAsync(x => x.Id == p.Concluida)).ConcluidaEm);
        var ocorrencias = await leitura.Ocorrencias.Where(o => o.MissaoId == p.Recorrente).OrderBy(o => o.DataPrevista).ToListAsync();
        Assert.Equal(new[] { StatusOcorrencia.Atrasada, StatusOcorrencia.Pendente, StatusOcorrencia.Excluida, StatusOcorrencia.Excluida, StatusOcorrencia.Concluida }, ocorrencias.Select(o => o.Status));
        Assert.Equal(StatusOcorrencia.Excluida, (await leitura.Ocorrencias.SingleAsync(o => o.MissaoId == p.Pendente)).Status);
        Assert.Single(await leitura.MedicoesCampanhas.Where(m => m.CampanhaId == p.Campanha).ToListAsync());
        Assert.Single(await leitura.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
        Assert.Equal(500, (await leitura.Usuarios.SingleAsync(u => u.Id == p.Usuario)).SaldoPontos);
        Assert.Single(await leitura.MissoesIA.Where(m => m.CampanhaId == p.Campanha).ToListAsync());
        Assert.Equal(p.Concluida, (await leitura.MissoesIA.SingleAsync(m => m.CampanhaId == p.Campanha)).MissaoId);
        Assert.Empty((await new EpicoRepository(leitura).ListarComCampanhasEMissoesAsync(p.Usuario, default)).Single().Campanhas);
        Assert.Single(await new MissaoRepository(leitura).ListarAvulsasAsync(p.Usuario, default));
        Assert.Null(await new MissaoRepository(leitura).BuscarPorIdAsync(p.Concluida, p.Usuario, default));
        var indicadores = await new IndicadoresPessoaisRepository(leitura).CalcularAsync(p.Usuario, new(Hoje.AddDays(-7), Hoje.AddDays(7), "America/Sao_Paulo"), default);
        Assert.Equal(2, indicadores.Concluidas);
    }

    [FatoPostgres]
    public async Task EpicoExcluiTodasAsCampanhasInclusiveEncerradasSemAfetarOutroEpicoOuAvulsas()
    {
        var p = await PrepararAsync(); var outro = await PrepararAsync();
        long segunda;
        await using (var c = banco.NovoContexto())
        {
            var campanha = new Campanha { EpicoId = p.Epico, Titulo = "Já encerrada", Estado = EstadoCampanha.Concluida };
            c.Campanhas.Add(campanha); await c.SaveChangesAsync(); segunda = campanha.Id;
            await Epico(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Epico), default);
            await Epico(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Epico), default);
        }
        await using var leitura = banco.NovoContexto();
        Assert.All(await leitura.Campanhas.Where(x => x.EpicoId == p.Epico).ToListAsync(), x => Assert.Equal(EstadoCampanha.Excluida, x.Estado));
        Assert.NotNull(await leitura.Campanhas.FindAsync(segunda));
        Assert.Empty(await new EpicoRepository(leitura).ListarComCampanhasEMissoesAsync(p.Usuario, default));
        Assert.Single(await new EpicoRepository(leitura).ListarComCampanhasEMissoesAsync(outro.Usuario, default));
        Assert.Single(await new MissaoRepository(leitura).ListarAvulsasAsync(p.Usuario, default));
        Assert.Single(await leitura.MedicoesCampanhas.Where(x => x.CampanhaId == p.Campanha).ToListAsync());
    }

    [FatoPostgres]
    public async Task MissaoConcluidaRejeitaExclusaoInclusiveQuandoSomenteOcorrenciaRegistraConclusao()
    {
        var p = await PrepararAsync();
        await using var c = banco.NovoContexto();
        await Assert.ThrowsAsync<MissaoConcluidaException>(() => Missao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Concluida), default));
        (await c.Missoes.SingleAsync(x => x.Id == p.Concluida)).ConcluidaEm = null;
        await c.SaveChangesAsync();
        await Assert.ThrowsAsync<MissaoConcluidaException>(() => Missao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Concluida), default));
        Assert.Equal(StatusMissao.Ativa, (await c.Missoes.SingleAsync(x => x.Id == p.Concluida)).Status);
        Assert.Single(await c.MovimentacoesPontos.Where(m => m.UsuarioId == p.Usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task MissaoAvulsaERecorrenteInativaSomemDasListasSemApagarConclusoes()
    {
        var p = await PrepararAsync();
        await using (var c = banco.NovoContexto())
        {
            var avulsa = await c.Missoes.SingleAsync(x => x.UsuarioId == p.Usuario && x.CampanhaId == null);
            (await c.Missoes.SingleAsync(x => x.Id == p.Recorrente)).Status = StatusMissao.Inativa;
            await c.SaveChangesAsync();
            await Missao(c).ExecuteAsync(new(p.Usuario, p.Usuario, p.Recorrente), default);
            await Missao(c).ExecuteAsync(new(p.Usuario, p.Usuario, avulsa.Id), default);
            await Missao(c).ExecuteAsync(new(p.Usuario, p.Usuario, avulsa.Id), default);
        }
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await new MissaoRepository(leitura).ListarAvulsasAsync(p.Usuario, default));
        var epico = (await new EpicoRepository(leitura).ListarComCampanhasEMissoesAsync(p.Usuario, default)).Single();
        Assert.Equal(2, epico.Campanhas.Single().Missoes.Count);
        Assert.Equal(5, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == p.Recorrente));
        var manutencao = new EstenderJanelaDeOcorrenciasUseCase(new MissaoRepository(leitura), new OcorrenciaRepository(leitura), leitura, NullLogger<EstenderJanelaDeOcorrenciasUseCase>.Instance);
        await manutencao.ExecuteAsync(default);
        Assert.Equal(5, await leitura.Ocorrencias.CountAsync(o => o.MissaoId == p.Recorrente));
        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Conclusao(leitura).ExecuteAsync(new(p.Usuario, p.Usuario, p.Recorrente), default));
    }

    [FatoPostgres]
    public async Task ExclusoesIsolamTitularEIdentificadoresAusentes()
    {
        var p = await PrepararAsync(); var outro = await banco.CriarUsuarioAsync(0);
        await using var c = banco.NovoContexto();
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Epico(c).ExecuteAsync(new(outro, p.Usuario, p.Epico), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Campanha(c).ExecuteAsync(new(outro, p.Usuario, p.Campanha), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Missao(c).ExecuteAsync(new(outro, p.Usuario, p.Recorrente), default));
        foreach (var id in new[] { p.Epico, long.MaxValue }) await Assert.ThrowsAsync<EpicoNaoEncontradoException>(() => Epico(c).ExecuteAsync(new(outro, outro, id), default));
        foreach (var id in new[] { p.Campanha, long.MaxValue }) await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => Campanha(c).ExecuteAsync(new(outro, outro, id), default));
        foreach (var id in new[] { p.Recorrente, long.MaxValue }) await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => Missao(c).ExecuteAsync(new(outro, outro, id), default));
        Assert.Equal(EstadoEpico.EmAndamento, (await c.Epicos.SingleAsync(x => x.Id == p.Epico)).Estado);
    }

    private sealed class Intercalar(IUnitOfWork unidade, Func<Task> outraOperacao) : IUnitOfWork
    {
        public async Task SalvarAlteracoesAsync(CancellationToken ct) { await outraOperacao(); await unidade.SalvarAlteracoesAsync(ct); }
    }

    [FatoPostgres]
    public async Task ExclusaoQueLeuAntesDaConclusaoReverteCascataSemApagarCredito()
    {
        var p = await PrepararAsync();
        await using var c = banco.NovoContexto();
        var intercalada = new Intercalar(c, async () =>
        {
            await using var outro = banco.NovoContexto();
            await Conclusao(outro).ExecuteAsync(new(p.Usuario, p.Usuario, p.Pendente), default);
        });
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Epico(c, intercalada).ExecuteAsync(new(p.Usuario, p.Usuario, p.Epico), default));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(EstadoEpico.EmAndamento, (await leitura.Epicos.SingleAsync(x => x.Id == p.Epico)).Estado);
        Assert.Equal(EstadoCampanha.EmAndamento, (await leitura.Campanhas.SingleAsync(x => x.Id == p.Campanha)).Estado);
        Assert.Equal(530, (await leitura.Usuarios.SingleAsync(x => x.Id == p.Usuario)).SaldoPontos);
        Assert.Equal(2, await leitura.MovimentacoesPontos.CountAsync(x => x.UsuarioId == p.Usuario));
        Assert.Equal(StatusMissao.Ativa, (await leitura.Missoes.SingleAsync(x => x.Id == p.Recorrente)).Status);
    }

    [FatoPostgres]
    public async Task ConclusaoQueLeuAntesDaExclusaoReverteOcorrenciaCreditoESaldo()
    {
        var p = await PrepararAsync();
        await using var c = banco.NovoContexto();
        var intercalada = new Intercalar(c, async () =>
        {
            await using var outro = banco.NovoContexto();
            await Missao(outro).ExecuteAsync(new(p.Usuario, p.Usuario, p.Pendente), default);
        });
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => Conclusao(c, intercalada).ExecuteAsync(new(p.Usuario, p.Usuario, p.Pendente), default));
        await using var leitura = banco.NovoContexto();
        Assert.Equal(StatusMissao.Excluida, (await leitura.Missoes.SingleAsync(x => x.Id == p.Pendente)).Status);
        Assert.Equal(StatusOcorrencia.Excluida, (await leitura.Ocorrencias.SingleAsync(x => x.MissaoId == p.Pendente)).Status);
        Assert.Equal(500, (await leitura.Usuarios.SingleAsync(x => x.Id == p.Usuario)).SaldoPontos);
        Assert.Single(await leitura.MovimentacoesPontos.Where(x => x.UsuarioId == p.Usuario).ToListAsync());
    }

    [FatoPostgres]
    public async Task CriacaoQueLeuEpicoAntesDaExclusaoReverteCampanhaNova()
    {
        var p = await PrepararAsync();
        await using var c = banco.NovoContexto();
        var intercalada = new Intercalar(c, async () =>
        {
            await using var outro = banco.NovoContexto(); await Epico(outro).ExecuteAsync(new(p.Usuario, p.Usuario, p.Epico), default);
        });
        var criar = new CriarCampanhaUseCase(new EpicoRepository(c), new CampanhaRepository(c), intercalada, NullLogger<CriarCampanhaUseCase>.Instance, new Relogio());
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => criar.ExecuteAsync(new(p.Usuario, p.Usuario, p.Epico, "Concorrente", null, Dificuldade.Facil, Hoje, Hoje.AddDays(10), null, null), default));
        await using var leitura = banco.NovoContexto();
        Assert.Single(await leitura.Campanhas.Where(x => x.EpicoId == p.Epico).ToListAsync());
    }

    [FatoPostgres]
    public async Task ManutencaoQueLeuMissaoAvulsaAntesDaExclusaoNaoRecriaOcorrencias()
    {
        var usuario = await banco.CriarUsuarioAsync(0); long id;
        await using (var c = banco.NovoContexto())
        {
            var missao = new Missao { UsuarioId = usuario, Titulo = "Avulsa", Recorrencia = new() { Tipo = "DIARIA", DataInicio = DateOnly.FromDateTime(DateTime.UtcNow) } };
            c.Missoes.Add(missao); await c.SaveChangesAsync(); id = missao.Id;
        }
        await using var manutencao = banco.NovoContexto();
        var intercalada = new Intercalar(manutencao, async () =>
        {
            await using var outro = banco.NovoContexto(); await Missao(outro).ExecuteAsync(new(usuario, usuario, id), default);
        });
        var rotina = new EstenderJanelaDeOcorrenciasUseCase(new MissaoRepository(manutencao), new OcorrenciaRepository(manutencao), intercalada, NullLogger<EstenderJanelaDeOcorrenciasUseCase>.Instance);
        await Assert.ThrowsAsync<DadosAtualizadosException>(() => rotina.ExecuteAsync(default));
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.Ocorrencias.Where(x => x.MissaoId == id).ToListAsync());
    }
}
