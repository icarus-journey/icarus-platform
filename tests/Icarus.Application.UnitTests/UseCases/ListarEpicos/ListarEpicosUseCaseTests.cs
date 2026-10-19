using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ListarEpicos;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ListarEpicos;

internal sealed class EpicoRepositorioEmMemoriaParaListar : IEpicoRepository
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
        => Task.FromResult(Epicos.Where(e => e.UsuarioId == usuarioId).OrderBy(e => e.Id).ToList());
}

public class ListarEpicosUseCaseTests
{
    private readonly EpicoRepositorioEmMemoriaParaListar _repositorio = new();
    private readonly ListarEpicosUseCase _useCase;

    public ListarEpicosUseCaseTests()
    {
        _useCase = new ListarEpicosUseCase(_repositorio, NullLogger<ListarEpicosUseCase>.Instance);
    }

    [Fact(DisplayName = "Lista os épicos do usuário com campanhas e missões aninhadas, ignorando os de outro usuário")]
    public async Task ExecuteAsync_DeveListarApenasEpicosDoUsuarioComGrafoAninhado()
    {
        var usuarioId = Guid.NewGuid();
        var missao = new Missao { Id = 1, UsuarioId = usuarioId, Titulo = "Missão", Dificuldade = Dificuldade.Facil, Pontuacao = 30 };
        var campanha = new Campanha
        {
            Id = 1, EpicoId = 1, Titulo = "Campanha", AreaDaVida = AreaDaVida.Financas, Dificuldade = Dificuldade.Media,
            Pontuacao = 45, PrazoInicio = new DateOnly(2026, 1, 1), PrazoFim = new DateOnly(2026, 12, 31), Missoes = [missao]
        };
        var meuEpico = new Epico
        {
            Id = 1, UsuarioId = usuarioId, Titulo = "Meu épico", AreaDaVida = AreaDaVida.Financas, Dificuldade = Dificuldade.Facil,
            Pontuacao = 30, PrazoInicio = new DateOnly(2026, 1, 1), PrazoFim = new DateOnly(2026, 12, 31), Campanhas = [campanha]
        };
        var epicoDeOutroUsuario = new Epico { Id = 2, UsuarioId = Guid.NewGuid(), Titulo = "Não é meu" };
        _repositorio.Epicos.Add(meuEpico);
        _repositorio.Epicos.Add(epicoDeOutroUsuario);

        var resultado = await _useCase.ExecuteAsync(new ListarEpicosCommand(usuarioId, usuarioId), CancellationToken.None);

        var epico = Assert.Single(resultado);
        Assert.Equal("Meu épico", epico.Titulo);
        var campanhaListada = Assert.Single(epico.Campanhas);
        Assert.Equal("Campanha", campanhaListada.Titulo);
        var missaoListada = Assert.Single(campanhaListada.Missoes);
        Assert.Equal("Missão", missaoListada.Titulo);
    }

    [Fact(DisplayName = "Usuário autenticado tentando listar épicos de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ListarEpicosCommand(meuId, idDeOutroUsuario), CancellationToken.None));
    }
}
