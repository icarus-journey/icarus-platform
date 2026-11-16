using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConcluirEpico;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ConcluirEpico;

internal sealed class EpicoRepositorioEmMemoriaParaConcluir : IEpicoRepository, IUnitOfWork
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

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class ConcluirEpicoUseCaseTests
{
    private readonly EpicoRepositorioEmMemoriaParaConcluir _repositorio = new();
    private readonly ConcluirEpicoUseCase _useCase;

    public ConcluirEpicoUseCaseTests()
    {
        _useCase = new ConcluirEpicoUseCase(_repositorio, _repositorio, NullLogger<ConcluirEpicoUseCase>.Instance);
    }

    [Fact(DisplayName = "Conclui o épico sem alterar o estado das campanhas (RF-03.8)")]
    public async Task ExecuteAsync_DeveConcluirSemCascata()
    {
        var usuarioId = Guid.NewGuid();
        var campanhaAberta = new Campanha { Id = 1, EpicoId = 1, Titulo = "C", Estado = EstadoCampanha.EmAndamento };
        var epico = new Epico { Id = 1, UsuarioId = usuarioId, Titulo = "E", Estado = EstadoEpico.EmAndamento, Campanhas = [campanhaAberta] };
        _repositorio.Epicos.Add(epico);

        var resultado = await _useCase.ExecuteAsync(new ConcluirEpicoCommand(usuarioId, usuarioId, epico.Id), CancellationToken.None);

        Assert.Equal(EstadoEpico.Concluido, resultado.Estado);
        Assert.Equal(EstadoCampanha.EmAndamento, campanhaAberta.Estado); // não mudou
    }

    [Fact(DisplayName = "Concluir de novo um épico já concluído é idempotente")]
    public async Task ExecuteAsync_DeveSerIdempotente()
    {
        var usuarioId = Guid.NewGuid();
        var epico = new Epico { Id = 1, UsuarioId = usuarioId, Titulo = "E", Estado = EstadoEpico.Concluido };
        _repositorio.Epicos.Add(epico);

        var resultado = await _useCase.ExecuteAsync(new ConcluirEpicoCommand(usuarioId, usuarioId, epico.Id), CancellationToken.None);

        Assert.Equal(EstadoEpico.Concluido, resultado.Estado);
    }

    [Fact(DisplayName = "Usuário autenticado tentando concluir épico de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ConcluirEpicoCommand(meuId, idDeOutroUsuario, 1), CancellationToken.None));
    }

    [Fact(DisplayName = "Épico inexistente (ou de outro usuário) lança EpicoNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoEpicoNaoExiste()
    {
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<EpicoNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ConcluirEpicoCommand(usuarioId, usuarioId, 999), CancellationToken.None));
    }
}
