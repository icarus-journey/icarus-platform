using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConcluirCampanha;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ConcluirCampanha;

internal sealed class CampanhaRepositorioEmMemoriaParaConcluir : ICampanhaRepository, IUnitOfWork
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

public class ConcluirCampanhaUseCaseTests
{
    private readonly CampanhaRepositorioEmMemoriaParaConcluir _repositorio = new();
    private readonly ConcluirCampanhaUseCase _useCase;

    public ConcluirCampanhaUseCaseTests()
    {
        _useCase = new ConcluirCampanhaUseCase(_repositorio, _repositorio, NullLogger<ConcluirCampanhaUseCase>.Instance);
    }

    [Fact(DisplayName = "Conclui a campanha mesmo sem o valor mensurado atingido (confirmação manual, RF-03.7)")]
    public async Task ExecuteAsync_DeveConcluirIndependenteDoValorAtual()
    {
        var usuarioId = Guid.NewGuid();
        var campanha = new Campanha { Id = 1, EpicoId = 1, Titulo = "C", ValorMensurado = 100m, ValorAtual = 10m, Estado = EstadoCampanha.EmAndamento };
        _repositorio.Campanhas.Add(campanha);

        var resultado = await _useCase.ExecuteAsync(new ConcluirCampanhaCommand(usuarioId, usuarioId, campanha.Id), CancellationToken.None);

        Assert.Equal(EstadoCampanha.Concluida, resultado.Estado);
        Assert.Equal(EstadoCampanha.Concluida, campanha.Estado);
    }

    [Fact(DisplayName = "Concluir de novo uma campanha já concluída é idempotente")]
    public async Task ExecuteAsync_DeveSerIdempotente()
    {
        var usuarioId = Guid.NewGuid();
        var campanha = new Campanha { Id = 1, EpicoId = 1, Titulo = "C", Estado = EstadoCampanha.Concluida };
        _repositorio.Campanhas.Add(campanha);

        var resultado = await _useCase.ExecuteAsync(new ConcluirCampanhaCommand(usuarioId, usuarioId, campanha.Id), CancellationToken.None);

        Assert.Equal(EstadoCampanha.Concluida, resultado.Estado);
    }

    [Fact(DisplayName = "Usuário autenticado tentando concluir campanha de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ConcluirCampanhaCommand(meuId, idDeOutroUsuario, 1), CancellationToken.None));
    }

    [Fact(DisplayName = "Campanha inexistente (ou de outro usuário) lança CampanhaNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoCampanhaNaoExiste()
    {
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(
            () => _useCase.ExecuteAsync(new ConcluirCampanhaCommand(usuarioId, usuarioId, 999), CancellationToken.None));
    }
}
