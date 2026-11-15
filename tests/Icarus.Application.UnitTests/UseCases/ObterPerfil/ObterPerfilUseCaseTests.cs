using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ObterPerfil;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ObterPerfil;

/// <summary>
/// Fake em memória — evita precisar de um Postgres real só para testar a regra do caso de uso.
/// Fica aqui, e não como classe pública reaproveitável, mesmo racional dos outros casos de uso.
/// </summary>
internal sealed class UsuarioRepositorioEmMemoriaParaPerfil : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = [];

    public Task<bool> ExisteComEmailAsync(string email, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.Any(u => u.Email == email));

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.SingleOrDefault(u => u.Email == email));

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.SingleOrDefault(u => u.Id == id));

    public Task AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        Usuarios.Add(usuario);
        return Task.CompletedTask;
    }
}

public class ObterPerfilUseCaseTests
{
    private readonly UsuarioRepositorioEmMemoriaParaPerfil _repositorio = new();
    private readonly ObterPerfilUseCase _useCase;

    public ObterPerfilUseCaseTests()
    {
        _useCase = new ObterPerfilUseCase(_repositorio, NullLogger<ObterPerfilUseCase>.Instance);
    }

    [Fact(DisplayName = "Usuário autenticado consultando o próprio id recebe o perfil")]
    public async Task ExecuteAsync_DeveDevolverPerfilQuandoIdsCoincidem()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Ana Silva",
            Email = "ana@exemplo.com",
            SenhaHash = "hash-irrelevante",
            DataNascimento = new DateOnly(2000, 1, 1)
        };
        _repositorio.Usuarios.Add(usuario);

        var resultado = await _useCase.ExecuteAsync(new ObterPerfilCommand(usuario.Id, usuario.Id), CancellationToken.None);

        Assert.Equal(usuario.Id, resultado.Id);
        Assert.Equal("Ana Silva", resultado.Nome);
        Assert.Equal("ana@exemplo.com", resultado.Email);
        Assert.Equal(new DateOnly(2000, 1, 1), resultado.DataNascimento);
    }

    [Fact(DisplayName = "Usuário autenticado consultando o id de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        _repositorio.Usuarios.Add(new Usuario
        {
            Id = idDeOutroUsuario,
            Nome = "Outro Usuario",
            Email = "outro@exemplo.com",
            SenhaHash = "hash-irrelevante",
            DataNascimento = new DateOnly(1999, 5, 5)
        });

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ObterPerfilCommand(meuId, idDeOutroUsuario), CancellationToken.None));
    }

    [Fact(DisplayName = "Id que não existe em lugar nenhum lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoUsuarioNaoExiste()
    {
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ObterPerfilCommand(id, id), CancellationToken.None));
    }
}
