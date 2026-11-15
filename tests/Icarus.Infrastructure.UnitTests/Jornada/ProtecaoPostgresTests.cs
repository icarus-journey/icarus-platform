using Icarus.Application.Common.Jornada;
using Icarus.Application.Common.Analise;
using Icarus.Application.UseCases.ConcluirMissao;
using Icarus.Application.UseCases.AtualizarFuso;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Messaging;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.UnitTests.Jornada;

public sealed class ProtecaoPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private static readonly DateOnly Inicio = new(2027, 1, 10);
    private static readonly string[] FaltaSemEstoque = ["protegido", "protegido", "interrompido", "interrompido"];
    private static readonly string[] RetroativoEncadeado = ["realizado", "protegido", "interrompido"];
    private sealed class Relogio(DateTimeOffset agora) : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = agora;
        public override DateTimeOffset GetUtcNow() => Agora;
    }
    private static DateTimeOffset Instante(int dia, int hora = 12) => new(2027, 1, dia, hora, 0, 0, TimeSpan.Zero);

    private async Task<Guid> Preparar(int unidades = 3, string? fuso = "America/Sao_Paulo", bool sequencia = true)
    {
        var id = await banco.CriarUsuarioAsync(5000);
        await using var c = banco.NovoContexto();
        var usuario = (await c.Usuarios.FindAsync(id))!;
        usuario.FusoHorario = fuso;
        usuario.ProtecaoAtivadaEm = Instante(10);
        var item = new Item { UsuarioId = id, Nome = "Proteção de sequência", Valor = 500, ControlaApp = ControlaApp.Sim };
        c.Itens.Add(item);
        for (var i = 0; i < unidades; i++) c.Set<ResgateItem>().Add(new()
        {
            UsuarioId = id,
            Item = item,
            OperacaoId = Guid.NewGuid(),
            Titulo = item.Nome,
            Custo = 500,
            Protecao = true,
            ResgatadoEm = Instante(9)
        });
        if (sequencia) Registrar(c, id, 9);
        await c.SaveChangesAsync();
        return id;
    }

    private static void Registrar(IcarusDbContext c, Guid id, int dia, string? fuso = "America/Sao_Paulo")
    {
        c.Ocorrencias.Add(new()
        {
            Missao = new() { UsuarioId = id, Titulo = "Missão do teste", Horario = Instante(dia), Pontuacao = 20 },
            DataPrevista = new(2027, 1, dia),
            Status = StatusOcorrencia.Concluida,
            RealizadaEm = Instante(dia),
            ConcluidaEm = Instante(dia, 13),
            FusoHorarioRealizacao = fuso
        });
    }

    private async Task Fechar(Guid id, Relogio relogio, Guid? trabalho = null)
    {
        await using var c = banco.NovoContexto();
        var protecoes = new ProtecaoSequenciaRepository(c, relogio);
        if (trabalho is { } chave)
            await new ProcessadorJornada(c, protecoes, relogio).ExecutarAsync(new("1.0.0", chave), default);
        else
            await new LojaRepository(c, protecoes).ExecutarAsync(id, (_, _) => Task.CompletedTask, default);
    }

    [FatoPostgres]
    public async Task FechaSomenteDiaEncerradoERepeticaoConcorrenteNaoDuplicaConsumo()
    {
        var id = await Preparar();
        var relogio = new Relogio(Instante(11, 2)); // Ainda dia 10 em São Paulo.
        await Fechar(id, relogio);
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == id).ToListAsync());
        relogio.Agora = Instante(11, 3); // Meia-noite local fecha o dia 10.
        await Task.WhenAll(Fechar(id, relogio), Fechar(id, relogio), Fechar(id, relogio));
        var consumo = Assert.Single(await leitura.Set<ConsumoProtecao>().AsNoTracking().Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Equal(Inicio, consumo.Data);
        Assert.Equal("America/Sao_Paulo", consumo.FusoHorario);
        Assert.Equal(2, await leitura.Set<ResgateItem>().CountAsync(x => x.UsuarioId == id && x.Estado == EstadoResgate.Resgatado));
        Assert.Single(await leitura.Set<FechamentoJornada>().Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Equal(5000, (await leitura.Usuarios.FindAsync(id))!.SaldoPontos);
        Assert.Empty(await leitura.MovimentacoesPontos.Where(x => x.UsuarioId == id).ToListAsync());
        var dados = (await new JornadaRepository(leitura).BuscarAsync(id, default))!;
        var sequencia = CalculadorSequencia.Calcular(dados, relogio.Agora);
        Assert.Equal(1, sequencia.DiasAtuais);
        Assert.False(sequencia.RealizouHoje);
        Assert.True(sequencia.Dias.Single(x => x.Data == Inicio).Protegida);
    }

    [FatoPostgres]
    public async Task ConsomeUmaPorDiaConsecutivoESemEstoqueInterrompe()
    {
        var id = await Preparar(2);
        await Fechar(id, new(Instante(14)));
        await using var c = banco.NovoContexto();
        var dias = await c.Set<FechamentoJornada>().Where(x => x.UsuarioId == id).OrderBy(x => x.Data).ToListAsync();
        Assert.Equal(FaltaSemEstoque, dias.Select(x => x.Estado));
        Assert.Equal(2, await c.Set<ConsumoProtecao>().CountAsync(x => x.UsuarioId == id));
        Assert.Equal(0, CalculadorSequencia.Calcular((await new JornadaRepository(c).BuscarAsync(id, default))!, Instante(14)).DiasAtuais);
    }

    [FatoPostgres]
    public async Task PoupaEstoqueSemSequenciaEQuandoJaHouveRealizacao()
    {
        var zerada = await Preparar(sequencia: false);
        var feita = await Preparar();
        await using (var c = banco.NovoContexto())
        {
            Registrar(c, feita, 10); Registrar(c, feita, 10); Registrar(c, feita, 10);
            await c.SaveChangesAsync();
        }
        await Fechar(zerada, new(Instante(11))); await Fechar(feita, new(Instante(11)));
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == zerada || x.UsuarioId == feita).ToListAsync());
        Assert.Equal(6, await leitura.Set<ResgateItem>().CountAsync(x => (x.UsuarioId == zerada || x.UsuarioId == feita) && x.Estado == EstadoResgate.Resgatado));
        Assert.Equal(2, CalculadorSequencia.Calcular((await new JornadaRepository(leitura).BuscarAsync(feita, default))!, Instante(11)).DiasAtuais);
    }

    [FatoPostgres]
    public async Task RetroativoDevolveUmaUnidadeSemReverterUsoOuGerarPontos()
    {
        var id = await Preparar(); var relogio = new Relogio(Instante(11));
        await Fechar(id, relogio);
        await using (var c = banco.NovoContexto()) { Registrar(c, id, 10); await c.SaveChangesAsync(); }
        await Fechar(id, relogio); await Fechar(id, relogio);
        await using var leitura = banco.NovoContexto();
        var consumo = Assert.Single(await leitura.Set<ConsumoProtecao>().Include(x => x.Resgate).Include(x => x.Devolucao).Where(x => x.UsuarioId == id).ToListAsync());
        Assert.NotNull(consumo.CompensadoEm); Assert.NotNull(consumo.Devolucao);
        Assert.Equal(EstadoResgate.Utilizado, consumo.Resgate.Estado);
        Assert.NotNull(consumo.Resgate.UtilizadoEm);
        Assert.Equal(consumo.Id, consumo.Devolucao.CompensacaoOrigemId);
        Assert.Equal(3, await leitura.Set<ResgateItem>().CountAsync(x => x.UsuarioId == id && x.Estado == EstadoResgate.Resgatado));
        Assert.Equal(5000, (await leitura.Usuarios.FindAsync(id))!.SaldoPontos);
        Assert.Empty(await leitura.MovimentacoesPontos.Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Equal("realizado", (await leitura.Set<FechamentoJornada>().SingleAsync(x => x.UsuarioId == id)).Estado);
        var indicadores = await new IndicadoresPessoaisRepository(leitura).CalcularAsync(id,
            new PeriodoAnalitico(new(2027, 1, 9), new(2027, 1, 11), "America/Sao_Paulo"), default);
        Assert.Equal(3, indicadores.RecompensasResgatadas);
        Assert.Null(indicadores.RecompensasUtilizadas);
        Assert.Equal(5, indicadores.QuantidadeRegistros); // Duas conclusões e três compras, sem inflar com devolução.

    }

    [FatoPostgres]
    public async Task DevolucaoPendenteRespeitaLimiteEReentraAssimQueAbreVaga()
    {
        var id = await Preparar(); var relogio = new Relogio(Instante(11));
        await Fechar(id, relogio);
        await using (var c = banco.NovoContexto())
        {
            var origem = await c.Set<ResgateItem>().FirstAsync(x => x.UsuarioId == id);
            c.Set<ResgateItem>().Add(new() { UsuarioId = id, ItemId = origem.ItemId, OperacaoId = Guid.NewGuid(), Titulo = origem.Titulo, Custo = 500, Protecao = true, ResgatadoEm = Instante(11) });
            Registrar(c, id, 10); await c.SaveChangesAsync();
        }
        await Fechar(id, relogio);
        await using (var c = banco.NovoContexto())
        {
            var consumo = await c.Set<ConsumoProtecao>().SingleAsync(x => x.UsuarioId == id);
            Assert.NotNull(consumo.CompensadoEm); Assert.Null(consumo.DevolucaoId);
            Assert.Equal(1, (await new JornadaRepository(c).BuscarAsync(id, default))!.ProtecoesPendentes);
            Assert.Equal(3, await c.Set<ResgateItem>().CountAsync(x => x.UsuarioId == id && x.Estado == EstadoResgate.Resgatado));
        }
        relogio.Agora = Instante(12);
        await Fechar(id, relogio); await Fechar(id, relogio);
        await using var leitura = banco.NovoContexto();
        Assert.Equal(0, (await new JornadaRepository(leitura).BuscarAsync(id, default))!.ProtecoesPendentes);
        Assert.Equal(3, await leitura.Set<ResgateItem>().CountAsync(x => x.UsuarioId == id && x.Estado == EstadoResgate.Resgatado));
        Assert.Equal(2, await leitura.Set<ConsumoProtecao>().CountAsync(x => x.UsuarioId == id));
    }

    [FatoPostgres]
    public async Task ReprocessaDiasSeguintesUsandoAUnidadeDevolvida()
    {
        var id = await Preparar(1); var relogio = new Relogio(Instante(13));
        await Fechar(id, relogio);
        await using (var c = banco.NovoContexto()) { Registrar(c, id, 10); await c.SaveChangesAsync(); }
        await Fechar(id, relogio);
        await using var leitura = banco.NovoContexto();
        var dias = await leitura.Set<FechamentoJornada>().Where(x => x.UsuarioId == id).OrderBy(x => x.Data).ToListAsync();
        Assert.Equal(RetroativoEncadeado, dias.Select(x => x.Estado));
        Assert.Equal(2, await leitura.Set<ConsumoProtecao>().CountAsync(x => x.UsuarioId == id));
        Assert.Single(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == id && x.CompensadoEm == null).ToListAsync());
    }

    [FatoPostgres]
    public async Task NaoUsaCompraPosteriorNemCobreFaltasAnterioresAAtivacao()
    {
        var id = await Preparar(1);
        await using (var c = banco.NovoContexto())
        {
            (await c.Set<ResgateItem>().SingleAsync(x => x.UsuarioId == id)).ResgatadoEm = Instante(11, 4);
            Registrar(c, id, 5); await c.SaveChangesAsync();
        }
        await Fechar(id, new(Instante(12)));
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Equal(2, await leitura.Set<FechamentoJornada>().CountAsync(x => x.UsuarioId == id));
        Assert.False(await leitura.Set<FechamentoJornada>().AnyAsync(x => x.UsuarioId == id && x.Data < Inicio));
    }

    [FatoPostgres]
    public async Task LegadoIncertoEFusoAusenteNaoGastamEstoque()
    {
        var incerta = await Preparar(sequencia: false); var semFuso = await Preparar(fuso: null);
        await using (var c = banco.NovoContexto()) { Registrar(c, incerta, 9, null); await c.SaveChangesAsync(); }
        await Fechar(incerta, new(Instante(11))); await Fechar(semFuso, new(Instante(11)));
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == incerta || x.UsuarioId == semFuso).ToListAsync());
        Assert.Equal("indeterminado", (await leitura.Set<FechamentoJornada>().SingleAsync(x => x.UsuarioId == incerta)).Estado);
        Assert.Empty(await leitura.Set<FechamentoJornada>().Where(x => x.UsuarioId == semFuso).ToListAsync());
    }

    [FatoPostgres]
    public async Task OutboxConfirmaAplicacaoUmaVezEIgnoraIdentificadorDesconhecido()
    {
        var id = await Preparar(); var relogio = new Relogio(Instante(11)); Guid trabalho;
        await using (var c = banco.NovoContexto())
        {
            new ProtecaoSequenciaRepository(c, relogio).SolicitarRecalculo(id);
            await c.SaveChangesAsync(); trabalho = (await c.Set<OutboxJornada>().SingleAsync(x => x.UsuarioId == id)).Id;
        }
        await Task.WhenAll(Fechar(id, relogio, trabalho), Fechar(id, relogio, trabalho));
        await Fechar(id, relogio, Guid.NewGuid());
        await using var leitura = banco.NovoContexto();
        Assert.NotNull((await leitura.Set<OutboxJornada>().SingleAsync(x => x.Id == trabalho)).ConcluidaEm);
        Assert.Single(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == id).ToListAsync());
    }

    [FatoPostgres]
    public async Task FalhaAposFechamentoReverteConsumoEDecisaoNaMesmaTransacao()
    {
        var id = await Preparar(); var relogio = new Relogio(Instante(11));
        await using var c = banco.NovoContexto();
        var loja = new LojaRepository(c, new ProtecaoSequenciaRepository(c, relogio));
        await Assert.ThrowsAsync<InvalidOperationException>(() => loja.ExecutarAsync(id, (_, _) => throw new InvalidOperationException("Falha antes do commit"), default));
        Assert.Empty(await c.Set<ConsumoProtecao>().Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Empty(await c.Set<FechamentoJornada>().Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Equal(3, await c.Set<ResgateItem>().CountAsync(x => x.UsuarioId == id && x.Estado == EstadoResgate.Resgatado));
    }


    [FatoPostgres]
    public async Task MissaoRecentePreservaSequenciaMesmoComContagemAntigaIncerta()
    {
        var id = await Preparar();
        await using (var c = banco.NovoContexto()) { Registrar(c, id, 8, null); await c.SaveChangesAsync(); }
        await Fechar(id, new(Instante(11)));
        await using var leitura = banco.NovoContexto();
        Assert.Single(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Equal("protegido", (await leitura.Set<FechamentoJornada>().SingleAsync(x => x.UsuarioId == id)).Estado);
        Assert.Null(CalculadorSequencia.Calcular((await new JornadaRepository(leitura).BuscarAsync(id, default))!, Instante(11)).DiasAtuais);
    }

    [FatoPostgres]
    public async Task TrocaDeFusoFechaNoAnteriorEPreservaDecisoesHistoricas()
    {
        var id = await Preparar(); var relogio = new Relogio(Instante(11, 4));
        await using (var c = banco.NovoContexto())
        {
            var atualizar = new AtualizarFusoUseCase(new UsuarioRepository(c), c, NullLogger<AtualizarFusoUseCase>.Instance,
                new TransacaoConclusaoMissao(c), new ProtecaoSequenciaRepository(c, relogio), relogio);
            await atualizar.ExecuteAsync(new(id, id, "Asia/Tokyo"), default);
        }
        relogio.Agora = Instante(12);
        await Fechar(id, relogio);
        await using var leitura = banco.NovoContexto();
        var dias = await leitura.Set<FechamentoJornada>().Where(x => x.UsuarioId == id).OrderBy(x => x.Data).ToListAsync();
        Assert.Equal("America/Sao_Paulo", dias[0].FusoHorario);
        Assert.Equal("Asia/Tokyo", dias[1].FusoHorario);
        Assert.Equal(2, await leitura.Set<ConsumoProtecao>().CountAsync(x => x.UsuarioId == id));
        Assert.Equal("America/Sao_Paulo", (await leitura.Usuarios.FindAsync(id))!.FusoInicioProtecao);
    }

    [FatoPostgres]
    public async Task ConclusaoEOutboxSaoAtomicasERetryNaoCriaNovoPedido()
    {
        var id = await Preparar(); var relogio = new Relogio(Instante(11)); long missao; long ocorrencia;
        await using (var c = banco.NovoContexto())
        {
            var m = new Missao
            {
                UsuarioId = id,
                Titulo = "Missão retroativa",
                Pontuacao = 20,
                DataLimite = Inicio,
                Ocorrencias = [new() { DataPrevista = Inicio }]
            };
            c.Missoes.Add(m); await c.SaveChangesAsync(); missao = m.Id; ocorrencia = m.Ocorrencias.Single().Id;
        }
        ConcluirMissaoUseCase Concluir(IcarusDbContext c, IUnitOfWork? unidade = null) => new(new MissaoRepository(c), new OcorrenciaRepository(c),
            new MovimentacaoPontosRepository(c), new CampanhaRepository(c), new UsuarioRepository(c), unidade ?? c,
            new TransacaoConclusaoMissao(c), NullLogger<ConcluirMissaoUseCase>.Instance, relogio,
            protecoes: new ProtecaoSequenciaRepository(c, relogio));
        var comando = new ConcluirMissaoCommand(id, id, missao, ocorrencia, Inicio, new(9, 0));
        await using (var c = banco.NovoContexto())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => Concluir(c, new UnidadeComFalha(c)).ExecuteAsync(comando, default));
            Assert.Empty(await c.Set<OutboxJornada>().Where(x => x.UsuarioId == id).ToListAsync());
            Assert.Empty(await c.MovimentacoesPontos.Where(x => x.UsuarioId == id).ToListAsync());
            Assert.Null((await c.Ocorrencias.FindAsync(ocorrencia))!.RealizadaEm);
        }
        await using (var c = banco.NovoContexto())
        {
            await Concluir(c).ExecuteAsync(comando, default);
            await Concluir(c).ExecuteAsync(comando, default);
        }
        await using var leitura = banco.NovoContexto();
        var pedido = Assert.Single(await leitura.Set<OutboxJornada>().Where(x => x.UsuarioId == id).ToListAsync());
        Assert.Null(pedido.ConcluidaEm);
        Assert.Single(await leitura.MovimentacoesPontos.Where(x => x.UsuarioId == id).ToListAsync());
        await Fechar(id, relogio, pedido.Id);
        Assert.NotNull((await leitura.Set<OutboxJornada>().AsNoTracking().SingleAsync(x => x.Id == pedido.Id)).ConcluidaEm);
    }


    [FatoPostgres]
    public async Task OrdemDeChegadaDoRetroativoNaoMudaOsDiasCobertos()
    {
        var tardia = await Preparar(1, sequencia: false);
        var completa = await Preparar(1, sequencia: false);
        var relogio = new Relogio(Instante(13));
        await using (var c = banco.NovoContexto())
        {
            Registrar(c, tardia, 11); Registrar(c, completa, 11); Registrar(c, completa, 9);
            await c.SaveChangesAsync();
        }
        await Fechar(tardia, relogio);
        await using (var c = banco.NovoContexto()) { Registrar(c, tardia, 9); await c.SaveChangesAsync(); }
        await Fechar(tardia, relogio); await Fechar(completa, relogio);
        await Fechar(tardia, relogio);
        await using var leitura = banco.NovoContexto();
        var a = await leitura.Set<FechamentoJornada>().Where(x => x.UsuarioId == tardia).OrderBy(x => x.Data).Select(x => x.Estado).ToListAsync();
        var b = await leitura.Set<FechamentoJornada>().Where(x => x.UsuarioId == completa).OrderBy(x => x.Data).Select(x => x.Estado).ToListAsync();
        Assert.Equal(b, a);
        Assert.Equal("protegido", a[0]); Assert.Equal("realizado", a[1]); Assert.Equal("interrompido", a[2]);
        var historico = await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == tardia).ToListAsync();
        Assert.Equal(2, historico.Count); Assert.Single(historico, x => x.CompensadoEm == null);
    }


    [FatoPostgres]
    public async Task BrokerIndisponivelMantemOutboxParaReenvio()
    {
        var id = await Preparar(); var relogio = new Relogio(Instante(11)); Guid chave;
        await using (var c = banco.NovoContexto())
        {
            new ProtecaoSequenciaRepository(c, relogio).SolicitarRecalculo(id);
            await c.SaveChangesAsync(); chave = (await c.Set<OutboxJornada>().SingleAsync(x => x.UsuarioId == id)).Id;
        }
        var servicos = new ServiceCollection();
        servicos.AddDbContext<IcarusDbContext>(o => o.UseNpgsql(banco.Conexao).UseSnakeCaseNamingConvention());
        await using var provedor = servicos.BuildServiceProvider();
        var configuracao = Options.Create(new RabbitMqOptions { Host = "127.0.0.1", Porta = 1 });
        await using var conexoes = new RabbitMqConnectionProvider(configuracao);
        var publicador = new PublicadorOutboxJornada(provedor.GetRequiredService<IServiceScopeFactory>(), conexoes,
            configuracao, relogio, NullLogger<PublicadorOutboxJornada>.Instance);
        using var prazo = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAnyAsync<Exception>(() => publicador.PublicarAsync(prazo.Token));
        await using var leitura = banco.NovoContexto();
        var pedido = await leitura.Set<OutboxJornada>().SingleAsync(x => x.Id == chave);
        Assert.Null(pedido.ConcluidaEm); Assert.Null(pedido.ProximaTentativaEm);
        Assert.Empty(await leitura.Set<ConsumoProtecao>().Where(x => x.UsuarioId == id).ToListAsync());
    }

    private sealed class UnidadeComFalha(IcarusDbContext c) : IUnitOfWork
    {
        public async Task SalvarAlteracoesAsync(CancellationToken ct)
        {
            await c.SaveChangesAsync(ct);
            throw new InvalidOperationException("Falha depois de gravar, antes do commit");
        }
    }

    [Theory]
    [InlineData("2018-11-04", "America/Sao_Paulo", "2018-11-04T03:00:00Z")]
    [InlineData("2026-10-31", "Asia/Tokyo", "2026-10-30T15:00:00Z")]
    public void InicioCivilRespeitaHorarioDeVeraoEFusos(string dia, string fuso, string esperado)
        => Assert.Equal(DateTimeOffset.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture),
            ProtecaoSequenciaRepository.InicioDia(DateOnly.Parse(dia, System.Globalization.CultureInfo.InvariantCulture), fuso));
}
