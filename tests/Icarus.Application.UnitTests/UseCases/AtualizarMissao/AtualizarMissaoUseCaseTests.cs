using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.AtualizarMissao;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.AtualizarMissao;

internal sealed class MissaoRepositorioEmMemoriaParaAtualizar : IMissaoRepository, IUnitOfWork
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

public class AtualizarMissaoUseCaseTests
{
    private readonly MissaoRepositorioEmMemoriaParaAtualizar _repositorio = new();
    private readonly AtualizarMissaoUseCase _useCase;

    public AtualizarMissaoUseCaseTests()
    {
        _useCase = new AtualizarMissaoUseCase(_repositorio, _repositorio, NullLogger<AtualizarMissaoUseCase>.Instance);
    }

    [Fact(DisplayName = "Atualiza missão recalculando a pontuação a partir da nova dificuldade")]
    public async Task ExecuteAsync_DeveAtualizarRecalculandoPontuacao()
    {
        var missao = new Missao { Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Antiga", Dificuldade = Dificuldade.Trivial, Pontuacao = 15 };
        _repositorio.Missoes.Add(missao);

        var command = new AtualizarMissaoCommand(
            missao.UsuarioId, missao.UsuarioId, missao.Id, "Nova", "descrição", "benefício", Dificuldade.Media, null, "20:00");

        var resultado = await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal("Nova", resultado.Titulo);
        Assert.Equal(45, resultado.Pontuacao); // Media
        Assert.Equal(new TimeOnly(20, 0), TimeOnly.FromDateTime(resultado.Horario.DateTime));
    }

    [Fact(DisplayName = "Missão já concluída é imutável — edição lança MissaoConcluidaException (RF-04.3)")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoMissaoJaConcluida()
    {
        var missao = new Missao
        {
            Id = 1, UsuarioId = Guid.NewGuid(), Titulo = "Concluída", Dificuldade = Dificuldade.Facil,
            Pontuacao = 30, ConcluidaEm = DateTimeOffset.UtcNow
        };
        _repositorio.Missoes.Add(missao);

        var command = new AtualizarMissaoCommand(
            missao.UsuarioId, missao.UsuarioId, missao.Id, "Novo título", null, null, Dificuldade.Media, null, null);

        await Assert.ThrowsAsync<MissaoConcluidaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
        Assert.Equal("Concluída", missao.Titulo); // não foi alterada
    }

    [Fact(DisplayName = "Missão inexistente (ou de outro usuário) lança MissaoNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoMissaoNaoExiste()
    {
        var usuarioId = Guid.NewGuid();
        var command = new AtualizarMissaoCommand(usuarioId, usuarioId, 999, "Título", null, null, Dificuldade.Facil, null, null);

        await Assert.ThrowsAsync<MissaoNaoEncontradaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando editar missão de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var command = new AtualizarMissaoCommand(meuId, idDeOutroUsuario, 1, "Título", null, null, Dificuldade.Facil, null, null);

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }
}
