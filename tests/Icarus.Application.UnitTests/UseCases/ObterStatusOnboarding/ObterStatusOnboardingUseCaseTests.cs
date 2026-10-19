using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ObterStatusOnboarding;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ObterStatusOnboarding;

internal sealed class TrabalhoOnboardingRepositorioEmMemoriaParaStatus : ITrabalhoOnboardingRepository
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
}

internal sealed class EpicoRepositorioEmMemoriaParaStatus : IEpicoRepository
{
    public List<Epico> Epicos { get; } = [];

    public Task AdicionarAsync(Epico epico, CancellationToken cancellationToken)
    {
        Epicos.Add(epico);
        return Task.CompletedTask;
    }

    public Task<Epico?> BuscarComCampanhaEMissoesAsync(long epicoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Epicos.SingleOrDefault(e => e.Id == epicoId && e.UsuarioId == usuarioId));
}

public class ObterStatusOnboardingUseCaseTests
{
    private readonly TrabalhoOnboardingRepositorioEmMemoriaParaStatus _trabalhoRepositorio = new();
    private readonly EpicoRepositorioEmMemoriaParaStatus _epicoRepositorio = new();
    private readonly ObterStatusOnboardingUseCase _useCase;

    public ObterStatusOnboardingUseCaseTests()
    {
        _useCase = new ObterStatusOnboardingUseCase(_trabalhoRepositorio, _epicoRepositorio, NullLogger<ObterStatusOnboardingUseCase>.Instance);
    }

    [Fact(DisplayName = "Trabalho pendente devolve só o status, sem épico")]
    public async Task ExecuteAsync_DeveDevolverApenasStatusQuandoPendente()
    {
        var usuarioId = Guid.NewGuid();
        var trabalho = new TrabalhoOnboarding { Id = Guid.NewGuid(), UsuarioId = usuarioId, Status = StatusTrabalhoOnboarding.Pendente };
        _trabalhoRepositorio.Trabalhos.Add(trabalho);

        var resultado = await _useCase.ExecuteAsync(new ObterStatusOnboardingCommand(usuarioId, usuarioId, trabalho.Id), CancellationToken.None);

        Assert.Equal(StatusTrabalhoOnboarding.Pendente, resultado.Status);
        Assert.Null(resultado.Epico);
        Assert.Null(resultado.ErroMensagem);
    }

    [Fact(DisplayName = "Trabalho falho devolve a mensagem de erro")]
    public async Task ExecuteAsync_DeveDevolverErroQuandoFalhou()
    {
        var usuarioId = Guid.NewGuid();
        var trabalho = new TrabalhoOnboarding
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Status = StatusTrabalhoOnboarding.Falhou,
            ErroMensagem = "worker indisponível"
        };
        _trabalhoRepositorio.Trabalhos.Add(trabalho);

        var resultado = await _useCase.ExecuteAsync(new ObterStatusOnboardingCommand(usuarioId, usuarioId, trabalho.Id), CancellationToken.None);

        Assert.Equal(StatusTrabalhoOnboarding.Falhou, resultado.Status);
        Assert.Null(resultado.Epico);
        Assert.Equal("worker indisponível", resultado.ErroMensagem);
    }

    [Fact(DisplayName = "Trabalho concluído devolve épico, campanha e missões aninhados")]
    public async Task ExecuteAsync_DeveDevolverResultadoAninhadoQuandoConcluido()
    {
        var usuarioId = Guid.NewGuid();

        var missao = new Missao
        {
            Id = 1,
            UsuarioId = usuarioId,
            Titulo = "Caminhada matinal",
            Dificuldade = Dificuldade.Facil,
            Pontuacao = 30,
            Horario = DateTimeOffset.UtcNow
        };
        var campanha = new Campanha
        {
            Id = 10,
            Titulo = "Manhãs ativas",
            AreaDaVida = AreaDaVida.CorpoESaude,
            Dificuldade = Dificuldade.Media,
            Pontuacao = 45,
            PrazoInicio = new DateOnly(2026, 10, 17),
            PrazoFim = new DateOnly(2027, 10, 17),
            Missoes = [missao]
        };
        var epico = new Epico
        {
            Id = 100,
            UsuarioId = usuarioId,
            Titulo = "Vida mais saudável",
            AreaDaVida = AreaDaVida.CorpoESaude,
            Dificuldade = Dificuldade.Facil,
            Pontuacao = 30,
            PrazoInicio = new DateOnly(2026, 10, 17),
            PrazoFim = new DateOnly(2027, 10, 17),
            Campanhas = [campanha]
        };
        _epicoRepositorio.Epicos.Add(epico);

        var trabalho = new TrabalhoOnboarding
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Status = StatusTrabalhoOnboarding.Concluido,
            EpicoId = epico.Id
        };
        _trabalhoRepositorio.Trabalhos.Add(trabalho);

        var resultado = await _useCase.ExecuteAsync(new ObterStatusOnboardingCommand(usuarioId, usuarioId, trabalho.Id), CancellationToken.None);

        Assert.Equal(StatusTrabalhoOnboarding.Concluido, resultado.Status);
        Assert.NotNull(resultado.Epico);
        Assert.Equal("Vida mais saudável", resultado.Epico.Titulo);
        Assert.Equal("Manhãs ativas", resultado.Epico.Campanha.Titulo);
        var missaoResultado = Assert.Single(resultado.Epico.Campanha.Missoes);
        Assert.Equal("Caminhada matinal", missaoResultado.Titulo);
        Assert.Equal(30, missaoResultado.Pontuacao);
    }

    [Fact(DisplayName = "Usuário autenticado tentando consultar onboarding de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ObterStatusOnboardingCommand(meuId, idDeOutroUsuario, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact(DisplayName = "Job inexistente (ou de outro usuário) lança TrabalhoOnboardingNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoJobNaoExiste()
    {
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<TrabalhoOnboardingNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ObterStatusOnboardingCommand(usuarioId, usuarioId, Guid.NewGuid()), CancellationToken.None));
    }
}
