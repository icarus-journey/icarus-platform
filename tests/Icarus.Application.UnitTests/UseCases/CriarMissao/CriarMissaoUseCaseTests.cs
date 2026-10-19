using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.CriarMissao;

internal sealed class CampanhaRepositorioEmMemoriaParaCriarMissao : ICampanhaRepository
{
    public List<Campanha> Campanhas { get; } = [];

    public Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        Campanhas.Add(campanha);
        return Task.CompletedTask;
    }

    public Task<Campanha?> BuscarPorIdAsync(long campanhaId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Campanhas.SingleOrDefault(c => c.Id == campanhaId));
}

internal sealed class MissaoRepositorioEmMemoriaParaCriar : IMissaoRepository, IUnitOfWork
{
    public List<Missao> Missoes { get; } = [];

    public Task AdicionarAsync(Missao missao, CancellationToken cancellationToken)
    {
        Missoes.Add(missao);
        return Task.CompletedTask;
    }

    public Task<Missao?> BuscarPorIdAsync(long missaoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.SingleOrDefault(m => m.Id == missaoId));

    public Task<List<Missao>> ListarAvulsasAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.UsuarioId == usuarioId && m.CampanhaId == null).ToList());

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class CriarMissaoUseCaseTests
{
    private readonly CampanhaRepositorioEmMemoriaParaCriarMissao _campanhaRepositorio = new();
    private readonly MissaoRepositorioEmMemoriaParaCriar _missaoRepositorio = new();
    private readonly CriarMissaoUseCase _useCase;

    public CriarMissaoUseCaseTests()
    {
        _useCase = new CriarMissaoUseCase(
            _campanhaRepositorio, _missaoRepositorio, _missaoRepositorio, NullLogger<CriarMissaoUseCase>.Instance);
    }

    [Fact(DisplayName = "Cria missão avulsa (sem campanha) calculando a pontuação e usando 09:00 como horário padrão")]
    public async Task ExecuteAsync_DeveCriarMissaoAvulsaComHorarioPadrao()
    {
        var usuarioId = Guid.NewGuid();
        var command = new CriarMissaoCommand(usuarioId, usuarioId, null, "Beber água", null, null, Dificuldade.Trivial, null, null);

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Null(resultado.CampanhaId);
        Assert.Equal(15, resultado.Pontuacao); // Trivial
        Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(resultado.Horario.DateTime));
    }

    [Fact(DisplayName = "Cria missão vinculada a uma campanha do usuário, respeitando o horário informado")]
    public async Task ExecuteAsync_DeveCriarMissaoVinculadaACampanha()
    {
        var usuarioId = Guid.NewGuid();
        var campanha = new Campanha { Id = 1, EpicoId = 1, Titulo = "Campanha", Dificuldade = Dificuldade.Media, Pontuacao = 45 };
        _campanhaRepositorio.Campanhas.Add(campanha);

        var command = new CriarMissaoCommand(
            usuarioId, usuarioId, campanha.Id, "Caminhada", "descrição", "benefício", Dificuldade.Facil, null, "07:30");

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(campanha.Id, resultado.CampanhaId);
        Assert.Equal(30, resultado.Pontuacao); // Facil
        Assert.Equal(new TimeOnly(7, 30), TimeOnly.FromDateTime(resultado.Horario.DateTime));
    }

    [Fact(DisplayName = "Campanha inexistente (ou de outro usuário) lança CampanhaNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoCampanhaNaoExiste()
    {
        var usuarioId = Guid.NewGuid();
        var command = new CriarMissaoCommand(usuarioId, usuarioId, 999, "Missão", null, null, Dificuldade.Facil, null, null);

        await Assert.ThrowsAsync<CampanhaNaoEncontradaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando criar missão para outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var command = new CriarMissaoCommand(meuId, idDeOutroUsuario, null, "Missão", null, null, Dificuldade.Facil, null, null);

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }
}
