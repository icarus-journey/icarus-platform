using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.CriarCampanha;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.CriarCampanha;

internal sealed class EpicoRepositorioEmMemoriaParaCriarCampanha : IEpicoRepository
{
    public List<Epico> Epicos { get; } = [];

    public Task AdicionarAsync(Epico epico, CancellationToken cancellationToken)
    {
        Epicos.Add(epico);
        return Task.CompletedTask;
    }

    public Task<Epico?> BuscarComCampanhaEMissoesAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Epicos.SingleOrDefault(e => e.Id == epicoId && e.UsuarioId == usuarioId));

    public Task<Epico?> BuscarPorIdAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Epicos.SingleOrDefault(e => e.Id == epicoId && e.UsuarioId == usuarioId));

    public Task<List<Epico>> ListarComCampanhasEMissoesAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Epicos.Where(e => e.UsuarioId == usuarioId).ToList());
}

internal sealed class CampanhaRepositorioEmMemoriaParaCriar : ICampanhaRepository, IUnitOfWork
{
    public List<Campanha> Campanhas { get; } = [];

    public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        Campanhas.Add(campanha);
        return Task.CompletedTask;
    }

    public Task<Campanha?> BuscarPorIdAsync(long campanhaId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Campanhas.SingleOrDefault(c => c.Id == campanhaId));

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class CriarCampanhaUseCaseTests
{
    private readonly EpicoRepositorioEmMemoriaParaCriarCampanha _epicoRepositorio = new();
    private readonly CampanhaRepositorioEmMemoriaParaCriar _campanhaRepositorio = new();
    private readonly CriarCampanhaUseCase _useCase;

    public CriarCampanhaUseCaseTests()
    {
        _useCase = new CriarCampanhaUseCase(
            _epicoRepositorio, _campanhaRepositorio, _campanhaRepositorio, NullLogger<CriarCampanhaUseCase>.Instance);
    }

    private Epico AdicionarEpico(Guid usuarioId, DateOnly prazoFim)
    {
        var epico = new Epico
        {
            Id = 1, UsuarioId = usuarioId, Titulo = "Épico", AreaDaVida = AreaDaVida.PropositoEEspiritualidade,
            Dificuldade = Dificuldade.Media, Pontuacao = 45, PrazoInicio = new DateOnly(2026, 1, 1), PrazoFim = prazoFim
        };
        _epicoRepositorio.Epicos.Add(epico);
        return epico;
    }

    [Fact(DisplayName = "Cria campanha herdando a área da vida do épico e calculando a pontuação a partir da dificuldade")]
    public async Task ExecuteAsync_DeveHerdarAreaDaVidaECalcularPontuacao()
    {
        var usuarioId = Guid.NewGuid();
        var epico = AdicionarEpico(usuarioId, new DateOnly(2026, 12, 31));

        var command = new CriarCampanhaCommand(
            usuarioId, usuarioId, epico.Id, "Campanha", null, Dificuldade.Facil, null, null, null, null);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(epico.AreaDaVida, resultado.AreaDaVida);
        Assert.Equal(30, resultado.Pontuacao); // Facil
        Assert.Equal(epico.PrazoFim, resultado.PrazoFim); // herdado do épico, já que não foi informado
        Assert.Null(resultado.ValorMensurado);
        Assert.Null(resultado.ValorAtual);
    }

    [Fact(DisplayName = "Épico inexistente (ou de outro usuário) lança EpicoNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoEpicoNaoExiste()
    {
        var usuarioId = Guid.NewGuid();
        var command = new CriarCampanhaCommand(usuarioId, usuarioId, 999, "Campanha", null, Dificuldade.Facil, null, null, null, null);

        await Assert.ThrowsAsync<EpicoNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando criar campanha para outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var command = new CriarCampanhaCommand(meuId, idDeOutroUsuario, 1, "Campanha", null, Dificuldade.Facil, null, null, null, null);

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }
}
