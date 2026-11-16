using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.IniciarOnboarding;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.IniciarOnboarding;

internal sealed class TrabalhoOnboardingRepositorioEmMemoriaParaIniciar : ITrabalhoOnboardingRepository, IUnitOfWork
{
    public List<TrabalhoOnboarding> Trabalhos { get; } = [];

    public Task AdicionarAsync(TrabalhoOnboarding trabalho, CancellationToken cancellationToken)
    {
        Trabalhos.Add(trabalho);
        return Task.CompletedTask;
    }

    public Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Trabalhos.SingleOrDefault(t => t.Id == id && t.UsuarioId == usuarioId));

    public Task<TrabalhoOnboarding?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Trabalhos.SingleOrDefault(t => t.Id == id));

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class PublisherFake : IOnboardingJobPublisher
{
    public List<(Guid JobId, Guid UsuarioId, IReadOnlyList<RespostaOnboarding> Respostas)> Publicados { get; } = [];

    public Task PublicarAsync(Guid jobId, Guid usuarioId, IReadOnlyList<RespostaOnboarding> respostas, CancellationToken cancellationToken)
    {
        Publicados.Add((jobId, usuarioId, respostas));
        return Task.CompletedTask;
    }
}

public class IniciarOnboardingUseCaseTests
{
    private readonly TrabalhoOnboardingRepositorioEmMemoriaParaIniciar _repositorio = new();
    private readonly PublisherFake _publisher = new();
    private readonly IniciarOnboardingUseCase _useCase;

    public IniciarOnboardingUseCaseTests()
    {
        _useCase = new IniciarOnboardingUseCase(_repositorio, _repositorio, _publisher, NullLogger<IniciarOnboardingUseCase>.Instance);
    }

    [Fact(DisplayName = "Usuário autenticado iniciando o próprio onboarding cria o trabalho e publica o job")]
    public async Task ExecuteAsync_DeveCriarTrabalhoEPublicarJob()
    {
        var usuarioId = Guid.NewGuid();
        var respostas = new List<RespostaOnboarding> { new("Qual a sua faixa etária?", ["18 a 24"]) };

        var resultado = await _useCase.ExecuteAsync(new IniciarOnboardingCommand(usuarioId, usuarioId, respostas), CancellationToken.None);

        var trabalho = Assert.Single(_repositorio.Trabalhos);
        Assert.Equal(resultado.JobId, trabalho.Id);
        Assert.Equal(usuarioId, trabalho.UsuarioId);
        Assert.Equal(StatusTrabalhoOnboarding.Pendente, trabalho.Status);

        var publicado = Assert.Single(_publisher.Publicados);
        Assert.Equal(resultado.JobId, publicado.JobId);
        Assert.Equal(usuarioId, publicado.UsuarioId);
        Assert.Same(respostas, publicado.Respostas);
    }

    [Fact(DisplayName = "Usuário autenticado tentando iniciar onboarding de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new IniciarOnboardingCommand(meuId, idDeOutroUsuario, []), CancellationToken.None));

        Assert.Empty(_repositorio.Trabalhos);
        Assert.Empty(_publisher.Publicados);
    }
}
