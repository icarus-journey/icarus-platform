using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.AtualizarCampanha;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.AtualizarCampanha;

internal sealed class CampanhaRepositorioEmMemoriaParaAtualizar : ICampanhaRepository, IUnitOfWork
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

public class AtualizarCampanhaUseCaseTests
{
    private readonly CampanhaRepositorioEmMemoriaParaAtualizar _repositorio = new();
    private readonly AtualizarCampanhaUseCase _useCase;

    public AtualizarCampanhaUseCaseTests()
    {
        _useCase = new AtualizarCampanhaUseCase(_repositorio, _repositorio, NullLogger<AtualizarCampanhaUseCase>.Instance);
    }

    [Fact(DisplayName = "Atualiza campanha recalculando a pontuação a partir da nova dificuldade")]
    public async Task ExecuteAsync_DeveAtualizarRecalculandoPontuacao()
    {
        var campanha = new Campanha
        {
            Id = 1, EpicoId = 1, Titulo = "Antiga", AreaDaVida = AreaDaVida.LazerECriatividade, Dificuldade = Dificuldade.Trivial,
            Pontuacao = 15, PrazoInicio = new DateOnly(2026, 1, 1), PrazoFim = new DateOnly(2026, 6, 1)
        };
        _repositorio.Campanhas.Add(campanha);

        var command = new AtualizarCampanhaCommand(
            Guid.Empty, Guid.Empty, campanha.Id, "Nova", "descrição", Dificuldade.Dificil,
            new DateOnly(2026, 2, 1), new DateOnly(2026, 8, 1), 100m, "pontos");

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal("Nova", resultado.Titulo);
        Assert.Equal(60, resultado.Pontuacao); // Dificil
        Assert.Equal(100m, resultado.ValorMensurado);
        Assert.Equal(0m, resultado.ValorAtual);
    }

    [Fact(DisplayName = "Campanha inexistente (ou de outro usuário) lança CampanhaNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoCampanhaNaoExiste()
    {
        var usuarioId = Guid.NewGuid();
        var command = new AtualizarCampanhaCommand(
            usuarioId, usuarioId, 999, "Título", null, Dificuldade.Facil, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), null, null);

        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando editar campanha de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var command = new AtualizarCampanhaCommand(
            meuId, idDeOutroUsuario, 1, "Título", null, Dificuldade.Facil, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), null, null);

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }
}
