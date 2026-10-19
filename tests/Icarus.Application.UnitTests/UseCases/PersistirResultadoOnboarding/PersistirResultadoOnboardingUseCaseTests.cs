using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.PersistirResultadoOnboarding;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.PersistirResultadoOnboarding;

internal sealed class TrabalhoOnboardingRepositorioEmMemoriaParaPersistir : ITrabalhoOnboardingRepository, IUnitOfWork
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

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        // Fake de IUnitOfWork: não há banco real, então simula o único efeito que o EF teria
        // aqui — gerar o Id do épico recém-adicionado (grafo setado via navegação, não FK).
        var proximoId = 1L;
        foreach (var trabalho in Trabalhos)
        {
            if (trabalho.Epico is { Id: 0 } epico)
            {
                epico.Id = proximoId++;
                trabalho.EpicoId = epico.Id;
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class EpicoRepositorioEmMemoriaParaPersistir : IEpicoRepository
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

public class PersistirResultadoOnboardingUseCaseTests
{
    private readonly TrabalhoOnboardingRepositorioEmMemoriaParaPersistir _trabalhoRepositorio = new();
    private readonly EpicoRepositorioEmMemoriaParaPersistir _epicoRepositorio = new();
    private readonly PersistirResultadoOnboardingUseCase _useCase;

    public PersistirResultadoOnboardingUseCaseTests()
    {
        _useCase = new PersistirResultadoOnboardingUseCase(
            _trabalhoRepositorio, _epicoRepositorio, _trabalhoRepositorio, NullLogger<PersistirResultadoOnboardingUseCase>.Instance);
    }

    private static PersistirResultadoOnboardingCommand ComandoValido(Guid jobId) => new(
        jobId,
        new EpicoResultadoOnboarding("Vida mais saudável", "descrição", AreaDaVida.CorpoESaude, Dificuldade.Facil, new DateOnly(2026, 10, 17), null),
        new CampanhaResultadoOnboarding("Manhãs ativas", "descrição", AreaDaVida.CorpoESaude, Dificuldade.Media, new DateOnly(2026, 10, 17), null),
        [
            new MissaoResultadoOnboarding("Caminhada matinal", "descrição", "benefício", Dificuldade.Facil, null, "07:00"),
            new MissaoResultadoOnboarding("Beber água", "descrição", "benefício", Dificuldade.Trivial, null, null)
        ]);

    [Fact(DisplayName = "Resultado válido persiste épico, campanha e missões com pontuação calculada da dificuldade")]
    public async Task ExecuteAsync_DevePersistirGrafoComPontuacaoCalculada()
    {
        var usuarioId = Guid.NewGuid();
        var trabalho = new TrabalhoOnboarding { Id = Guid.NewGuid(), UsuarioId = usuarioId, Status = StatusTrabalhoOnboarding.Pendente };
        _trabalhoRepositorio.Trabalhos.Add(trabalho);

        await _useCase.ExecuteAsync(ComandoValido(trabalho.Id), CancellationToken.None);

        Assert.Equal(StatusTrabalhoOnboarding.Concluido, trabalho.Status);
        Assert.NotNull(trabalho.EpicoId);

        var epico = Assert.Single(_epicoRepositorio.Epicos);
        Assert.Equal(usuarioId, epico.UsuarioId);
        Assert.Equal(30, epico.Pontuacao); // Facil
        Assert.Equal(AreaDaVida.CorpoESaude, epico.AreaDaVida);

        var campanha = Assert.Single(epico.Campanhas);
        Assert.Equal(45, campanha.Pontuacao); // Media
        Assert.Equal(AreaDaVida.CorpoESaude, campanha.AreaDaVida);

        Assert.Equal(2, campanha.Missoes.Count);
        Assert.Equal(30, campanha.Missoes.First(m => m.Titulo == "Caminhada matinal").Pontuacao); // Facil
        Assert.Equal(15, campanha.Missoes.First(m => m.Titulo == "Beber água").Pontuacao); // Trivial

        // DER seção 8: valor_mensurado = soma das missões — nunca confia em valor vindo de fora.
        Assert.Equal(45m, campanha.ValorMensurado);
    }

    [Fact(DisplayName = "Job inexistente é ignorado silenciosamente (reentrega sem trabalho correspondente)")]
    public async Task ExecuteAsync_DeveIgnorarQuandoJobNaoExiste()
    {
        await _useCase.ExecuteAsync(ComandoValido(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(_epicoRepositorio.Epicos);
    }

    [Fact(DisplayName = "Job já concluído é ignorado (idempotência — reentrega não duplica o épico)")]
    public async Task ExecuteAsync_DeveSerIdempotenteQuandoJaConcluido()
    {
        var usuarioId = Guid.NewGuid();
        var trabalho = new TrabalhoOnboarding
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Status = StatusTrabalhoOnboarding.Concluido,
            EpicoId = 999
        };
        _trabalhoRepositorio.Trabalhos.Add(trabalho);

        await _useCase.ExecuteAsync(ComandoValido(trabalho.Id), CancellationToken.None);

        Assert.Empty(_epicoRepositorio.Epicos);
        Assert.Equal(999, trabalho.EpicoId);
    }
}
