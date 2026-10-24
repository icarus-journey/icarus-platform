using System.Globalization;
using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarRelatorioSemanal;
using Icarus.Application.UseCases.PersistirRelatorioSemanal;
using Icarus.Application.UseCases.SolicitarRelatorioSemanal;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.UnitTests.Loja;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Infrastructure.UnitTests.Relatorios;

public sealed class RelatorioSemanalPostgresTests(BancoLoja banco) : IClassFixture<BancoLoja>
{
    private sealed class Relogio : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = DateTimeOffset.Parse("2026-10-20T12:00:00Z", CultureInfo.InvariantCulture);
        public override DateTimeOffset GetUtcNow() => Agora;
    }
    private readonly Relogio _relogio = new();
    private SolicitarRelatorioSemanalUseCase Solicitacao(IcarusDbContext contexto, IUnitOfWork? unidade = null) =>
        new(new RelatorioSemanalRepository(contexto), new IndicadoresPessoaisRepository(contexto), unidade ?? contexto,
            _relogio, NullLogger<SolicitarRelatorioSemanalUseCase>.Instance);
    private ConsultarRelatorioSemanalUseCase Consulta(IcarusDbContext contexto) =>
        new(new UsuarioRepository(contexto), new RelatorioSemanalRepository(contexto), _relogio);
    private PersistirRelatorioSemanalUseCase Persistencia(IcarusDbContext contexto) =>
        new(new RelatorioSemanalRepository(contexto), contexto, _relogio, NullLogger<PersistirRelatorioSemanalUseCase>.Instance);

    private async Task<Guid> PrepararAsync(bool comDados = true)
    {
        var id = await banco.CriarUsuarioAsync(300);
        await using var contexto = banco.NovoContexto();
        var usuario = await contexto.Usuarios.SingleAsync(u => u.Id == id);
        usuario.FusoHorario = "America/Sao_Paulo";
        if (comDados)
        {
            var missao = new Missao { UsuarioId = id, Titulo = "Título pessoal proibido", Descricao = "Diário ou texto livre proibido",
                Dificuldade = Dificuldade.Facil, Pontuacao = 30, DataLimite = new(2026, 10, 18), Horario = _relogio.Agora };
            contexto.Missoes.Add(missao);
            contexto.Ocorrencias.Add(new() { Missao = missao, DataPrevista = new(2026, 10, 18),
                Status = StatusOcorrencia.Concluida, RealizadaEm = DateTimeOffset.Parse("2026-10-19T02:59:59Z", CultureInfo.InvariantCulture), ConcluidaEm = _relogio.Agora });
            var fora = new Missao { UsuarioId = id, Titulo = "Fora do período", Dificuldade = Dificuldade.Facil, Horario = _relogio.Agora };
            contexto.Missoes.Add(fora);
            contexto.Ocorrencias.Add(new() { Missao = fora, DataPrevista = new(2026, 10, 19),
                Status = StatusOcorrencia.Concluida, RealizadaEm = DateTimeOffset.Parse("2026-10-19T03:00:00Z", CultureInfo.InvariantCulture), ConcluidaEm = _relogio.Agora });
        }
        await contexto.SaveChangesAsync();
        return id;
    }

    private async Task<SolicitacaoRelatorioSemanal> MensagemAsync(Guid usuarioId)
    {
        await using var contexto = banco.NovoContexto();
        var trabalho = await contexto.Set<TrabalhoRelatorioSemanal>().SingleAsync(t => t.UsuarioId == usuarioId);
        var carga = await contexto.Set<OutboxRelatorio>().Where(o => o.Id == trabalho.TentativaId).Select(o => o.Carga).SingleAsync();
        return JsonSerializer.Deserialize<SolicitacaoRelatorioSemanal>(carga!, ContratoRelatorioSemanal.Json)!;
    }

    private static ResultadoRelatorioSemanal Resultado(SolicitacaoRelatorioSemanal entrada, string resumo = "Você registrou uma conclusão no período.") =>
        new(Guid.NewGuid(), entrada.CorrelacaoId, entrada.IdMensagem, "relatorio.semanal.resultado", "1.0.0",
            entrada.CriadaEm.AddSeconds(1), entrada.ExpiraEm, entrada.ChaveIdempotencia, entrada.Periodo, "concluido",
            new("Seus registros da semana", resumo, "Considere revisar o próximo passo do seu planejamento.", "Qual pequeno ajuste faz sentido para você?"),
            null, new("ollama", "modelo-de-teste"));

    [FatoPostgres]
    public async Task SolicitaUmaVezSobConcorrenciaESemIdentidadeOuTextoLivreNaMensagem()
    {
        var id = await PrepararAsync();
        async Task Pedir() { await using var contexto = banco.NovoContexto(); await Solicitacao(contexto).ExecuteAsync(new(id, id), default); }
        await Task.WhenAll(Pedir(), Pedir());
        await using var leitura = banco.NovoContexto();
        Assert.Equal(1, await leitura.Set<TrabalhoRelatorioSemanal>().CountAsync(t => t.UsuarioId == id));
        var mensagem = await MensagemAsync(id);
        Assert.Equal(new DateOnly(2026, 10, 12), mensagem.Periodo.Inicio);
        Assert.Equal(1, mensagem.Indicadores.Concluidas);
        Assert.Null(mensagem.Indicadores.Previstas);
        Assert.Null(mensagem.Indicadores.TaxaConclusao);
        Assert.True(mensagem.Indicadores.HistoricoInsuficienteComparacao);
        var carga = JsonSerializer.Serialize(mensagem, ContratoRelatorioSemanal.Json);
        Assert.DoesNotContain("Título pessoal", carga);
        Assert.DoesNotContain("Diário", carga);
        Assert.DoesNotContain(id.ToString(), carga);
        Assert.DoesNotContain("email", carga);
        Assert.DoesNotContain("senha", carga);
        Assert.DoesNotContain("evolucao", carga);
        Assert.Equal(1, await leitura.Set<OutboxRelatorio>().CountAsync(o => o.TrabalhoId == mensagem.CorrelacaoId));
    }

    [FatoPostgres]
    public async Task ResultadoPersistidoSobreviveNovaConsultaEReentregaNaoSobrescreve()
    {
        var id = await PrepararAsync();
        await using (var contexto = banco.NovoContexto()) await Solicitacao(contexto).ExecuteAsync(new(id, id), default);
        var entrada = await MensagemAsync(id); var resultado = Resultado(entrada);
        await using (var contexto = banco.NovoContexto()) await Persistencia(contexto).ExecuteAsync(resultado, default);
        await using (var contexto = banco.NovoContexto()) await Persistencia(contexto).ExecuteAsync(resultado, default);
        await using (var contexto = banco.NovoContexto()) await Persistencia(contexto).ExecuteAsync(Resultado(entrada, "Outro texto não deve substituir o primeiro."), default);
        await using var leitura = banco.NovoContexto();
        var consulta = await Consulta(leitura).ExecuteAsync(new(id, id), default);
        Assert.Equal("disponivel", consulta.Estado);
        Assert.Equal(resultado.Conteudo!.Resumo, consulta.Relatorio!.Texto.Resumo);
        Assert.Single(await leitura.Relatorios.Where(r => r.UsuarioId == id).ToListAsync());
        Assert.Equal(2, await leitura.Set<InboxRelatorio>().CountAsync(i => i.TrabalhoId == entrada.CorrelacaoId));
        var repetir = await Solicitacao(leitura).ExecuteAsync(new(id, id, true), default);
        Assert.Equal(consulta.Relatorio.Id, repetir.Relatorio!.Id);
    }

    [FatoPostgres]
    public async Task FalhaOuResultadoInvalidoExigeNovaAcaoEResultadoAntigoNaoAplica()
    {
        var id = await PrepararAsync();
        await using (var contexto = banco.NovoContexto()) await Solicitacao(contexto).ExecuteAsync(new(id, id), default);
        var primeira = await MensagemAsync(id);
        await using (var contexto = banco.NovoContexto()) await Persistencia(contexto).ExecuteAsync(Resultado(primeira, "Diagnóstico e cura garantida."), default);
        await using (var contexto = banco.NovoContexto())
        {
            var falha = await Solicitacao(contexto).ExecuteAsync(new(id, id), default);
            Assert.Equal("falha", falha.Estado); Assert.True(falha.PodeTentarNovamente);
            Assert.Empty(await contexto.Relatorios.Where(r => r.UsuarioId == id).ToListAsync());
        }
        await using (var contexto = banco.NovoContexto()) await Solicitacao(contexto).ExecuteAsync(new(id, id, true), default);
        var segunda = await MensagemAsync(id);
        Assert.NotEqual(primeira.IdMensagem, segunda.IdMensagem);
        Assert.Equal(primeira.CorrelacaoId, segunda.CorrelacaoId);
        await using (var contexto = banco.NovoContexto()) await Persistencia(contexto).ExecuteAsync(Resultado(primeira), default);
        await using (var contexto = banco.NovoContexto())
        {
            Assert.Empty(await contexto.Relatorios.Where(r => r.UsuarioId == id).ToListAsync());
            await Persistencia(contexto).ExecuteAsync(Resultado(segunda), default);
        }
        await using var leitura = banco.NovoContexto();
        Assert.Equal("disponivel", (await Consulta(leitura).ExecuteAsync(new(id, id), default)).Estado);
    }

    [FatoPostgres]
    public async Task ExpiracaoNaoSalvaConteudoTardioEPermiteTentativaLimitada()
    {
        var id = await PrepararAsync();
        await using (var contexto = banco.NovoContexto()) await Solicitacao(contexto).ExecuteAsync(new(id, id), default);
        var entrada = await MensagemAsync(id); _relogio.Agora = _relogio.Agora.AddMinutes(6);
        await using (var contexto = banco.NovoContexto()) await Persistencia(contexto).ExecuteAsync(Resultado(entrada), default);
        await using (var contexto = banco.NovoContexto()) Assert.Empty(await contexto.Relatorios.Where(r => r.UsuarioId == id).ToListAsync());
        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            await using var contexto = banco.NovoContexto();
            await Solicitacao(contexto).ExecuteAsync(new(id, id, true), default);
            _relogio.Agora = _relogio.Agora.AddMinutes(6);
        }
        await using var leitura = banco.NovoContexto();
        Assert.Equal(3, (await leitura.Set<TrabalhoRelatorioSemanal>().SingleAsync(t => t.UsuarioId == id)).Tentativas);
        Assert.False((await Consulta(leitura).ExecuteAsync(new(id, id), default)).PodeTentarNovamente);
    }

    [FatoPostgres]
    public async Task IsolaTitularesEHistoriaInsuficienteNaoChamaProvedorNemInventaValores()
    {
        var id = await PrepararAsync(false);
        await using var contexto = banco.NovoContexto();
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Consulta(contexto).ExecuteAsync(new(Guid.NewGuid(), id), default));
        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => Solicitacao(contexto).ExecuteAsync(new(Guid.NewGuid(), id), default));
        var resultado = await Solicitacao(contexto).ExecuteAsync(new(id, id), default);
        Assert.Equal("historico_insuficiente", resultado.Estado);
        Assert.Empty(await contexto.Set<TrabalhoRelatorioSemanal>().Where(t => t.UsuarioId == id).ToListAsync());
        var indicadores = await new IndicadoresPessoaisRepository(contexto).CalcularAsync(id, new(new(2026, 10, 12), new(2026, 10, 18), "America/Sao_Paulo"), default);
        Assert.Null(indicadores.Concluidas); Assert.Null(indicadores.PontosGanhos); Assert.Null(indicadores.Sequencia);
    }

    [FatoPostgres]
    public async Task FalhaNaUnidadeDeTrabalhoReverteTrabalhoEOutbox()
    {
        var id = await PrepararAsync();
        await using (var contexto = banco.NovoContexto())
            await Assert.ThrowsAsync<InvalidOperationException>(() => Solicitacao(contexto, new UnidadeComFalha(contexto)).ExecuteAsync(new(id, id), default));
        await using var leitura = banco.NovoContexto();
        Assert.Empty(await leitura.Set<TrabalhoRelatorioSemanal>().Where(t => t.UsuarioId == id).ToListAsync());
    }
    private sealed class UnidadeComFalha(IcarusDbContext contexto) : IUnitOfWork
    {
        public async Task SalvarAlteracoesAsync(CancellationToken ct) { await contexto.SaveChangesAsync(ct); throw new InvalidOperationException("Falha simulada antes do commit."); }
    }
}
