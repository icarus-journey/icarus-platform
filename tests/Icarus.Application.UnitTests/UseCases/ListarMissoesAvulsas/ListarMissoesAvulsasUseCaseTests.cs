using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ListarMissoesAvulsas;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ListarMissoesAvulsas;

internal sealed class MissaoRepositorioEmMemoriaParaListarAvulsas : IMissaoRepository
{
    public List<Missao> Missoes { get; } = [];

    public Task AdicionarAsync(Missao missao, CancellationToken cancellationToken)
    {
        Missoes.Add(missao);
        return Task.CompletedTask;
    }

    public Task<Missao?> BuscarPorIdAsync(long missaoId, Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.SingleOrDefault(m => m.Id == missaoId && m.UsuarioId == usuarioId));

    public Task<List<Missao>> ListarAvulsasAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.UsuarioId == usuarioId && m.CampanhaId == null).ToList());

    public Task<List<Missao>> ListarRecorrentesAtivasAsync(CancellationToken cancellationToken)
        => Task.FromResult(Missoes.Where(m => m.Status == StatusMissao.Ativa && m.Recorrencia is not null).ToList());
}

public class ListarMissoesAvulsasUseCaseTests
{
    private readonly MissaoRepositorioEmMemoriaParaListarAvulsas _repositorio = new();
    private readonly ListarMissoesAvulsasUseCase _useCase;

    public ListarMissoesAvulsasUseCaseTests()
    {
        _useCase = new ListarMissoesAvulsasUseCase(_repositorio, NullLogger<ListarMissoesAvulsasUseCase>.Instance);
    }

    [Fact(DisplayName = "Lista apenas as missões avulsas (sem campanha) do usuário, ignorando as de outro usuário e as vinculadas")]
    public async Task ExecuteAsync_DeveListarApenasMissoesAvulsasDoUsuario()
    {
        var usuarioId = Guid.NewGuid();
        var avulsa = new Missao { Id = 1, UsuarioId = usuarioId, CampanhaId = null, Titulo = "Beber água", Dificuldade = Dificuldade.Trivial, Pontuacao = 15 };
        var vinculada = new Missao { Id = 2, UsuarioId = usuarioId, CampanhaId = 10, Titulo = "Vinculada", Dificuldade = Dificuldade.Facil, Pontuacao = 30 };
        var deOutroUsuario = new Missao { Id = 3, UsuarioId = Guid.NewGuid(), CampanhaId = null, Titulo = "Não é minha" };
        _repositorio.Missoes.Add(avulsa);
        _repositorio.Missoes.Add(vinculada);
        _repositorio.Missoes.Add(deOutroUsuario);

        var resultado = await _useCase.ExecuteAsync(new ListarMissoesAvulsasCommand(usuarioId, usuarioId), CancellationToken.None);

        var missao = Assert.Single(resultado);
        Assert.Equal("Beber água", missao.Titulo);
    }

    [Fact(DisplayName = "Usuário autenticado tentando listar missões avulsas de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ListarMissoesAvulsasCommand(meuId, idDeOutroUsuario), CancellationToken.None));
    }
}
