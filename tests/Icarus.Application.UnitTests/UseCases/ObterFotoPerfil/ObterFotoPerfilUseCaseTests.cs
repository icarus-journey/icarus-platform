using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ObterFotoPerfil;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.ObterFotoPerfil;

internal sealed class UsuarioRepositorioEmMemoriaParaObterFoto : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = [];

    public Task<bool> ExisteComEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult<Usuario?>(null);

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.SingleOrDefault(u => u.Id == id));

    public Task AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        Usuarios.Add(usuario);
        return Task.CompletedTask;
    }
}

internal sealed class ArmazenamentoDeFotoPerfilEmMemoria : IArmazenamentoDeFotoPerfil
{
    public Dictionary<string, (byte[] Conteudo, string ContentType)> Objetos { get; } = [];

    public Task<string> SalvarAsync(string chave, Stream conteudo, string contentType, long tamanho, CancellationToken cancellationToken)
        => throw new NotSupportedException("Não usado neste caso de uso.");

    public Task<(Stream Conteudo, string ContentType)> ObterAsync(string chave, CancellationToken cancellationToken)
    {
        var (conteudo, contentType) = Objetos[chave];
        return Task.FromResult<(Stream, string)>((new MemoryStream(conteudo), contentType));
    }
}

public class ObterFotoPerfilUseCaseTests
{
    private readonly UsuarioRepositorioEmMemoriaParaObterFoto _usuarioRepositorio = new();
    private readonly ArmazenamentoDeFotoPerfilEmMemoria _armazenamento = new();
    private readonly ObterFotoPerfilUseCase _useCase;

    public ObterFotoPerfilUseCaseTests()
    {
        _useCase = new ObterFotoPerfilUseCase(_usuarioRepositorio, _armazenamento, NullLogger<ObterFotoPerfilUseCase>.Instance);
    }

    [Fact(DisplayName = "Devolve a foto quando o usuário tem uma salva")]
    public async Task ExecuteAsync_DeveDevolverFotoQuandoExiste()
    {
        var usuarioId = Guid.NewGuid();
        _usuarioRepositorio.Usuarios.Add(new Usuario { Id = usuarioId, Nome = "Teste", Email = "teste@exemplo.com", FotoChave = usuarioId.ToString() });
        _armazenamento.Objetos[usuarioId.ToString()] = ([1, 2, 3], "image/jpeg");

        var resultado = await _useCase.ExecuteAsync(new ObterFotoPerfilCommand(usuarioId, usuarioId), CancellationToken.None);

        Assert.Equal("image/jpeg", resultado.ContentType);
    }

    [Fact(DisplayName = "Usuário sem foto lança FotoPerfilNaoEncontradaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoUsuarioNaoTemFoto()
    {
        var usuarioId = Guid.NewGuid();
        _usuarioRepositorio.Usuarios.Add(new Usuario { Id = usuarioId, Nome = "Teste", Email = "teste@exemplo.com" });

        await Assert.ThrowsAsync<FotoPerfilNaoEncontradaException>(
            () => _useCase.ExecuteAsync(new ObterFotoPerfilCommand(usuarioId, usuarioId), CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário inexistente lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoUsuarioNaoExiste()
    {
        var usuarioId = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ObterFotoPerfilCommand(usuarioId, usuarioId), CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando buscar foto de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(
            () => _useCase.ExecuteAsync(new ObterFotoPerfilCommand(meuId, idDeOutroUsuario), CancellationToken.None));
    }
}
