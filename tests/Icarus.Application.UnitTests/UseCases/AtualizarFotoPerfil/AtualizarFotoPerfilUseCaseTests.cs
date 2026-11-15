using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.AtualizarFotoPerfil;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.AtualizarFotoPerfil;

internal sealed class UsuarioRepositorioEmMemoriaParaFoto : IUsuarioRepository, IUnitOfWork
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

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class ArmazenamentoDeFotoPerfilEmMemoria : IArmazenamentoDeFotoPerfil
{
    public Dictionary<string, (byte[] Conteudo, string ContentType)> Objetos { get; } = [];

    public async Task<string> SalvarAsync(string chave, Stream conteudo, string contentType, long tamanho, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await conteudo.CopyToAsync(buffer, cancellationToken);
        Objetos[chave] = (buffer.ToArray(), contentType);
        return chave;
    }

    public Task<(Stream Conteudo, string ContentType)> ObterAsync(string chave, CancellationToken cancellationToken)
    {
        var (conteudo, contentType) = Objetos[chave];
        return Task.FromResult<(Stream, string)>((new MemoryStream(conteudo), contentType));
    }
}

public class AtualizarFotoPerfilUseCaseTests
{
    private readonly UsuarioRepositorioEmMemoriaParaFoto _usuarioRepositorio = new();
    private readonly ArmazenamentoDeFotoPerfilEmMemoria _armazenamento = new();
    private readonly AtualizarFotoPerfilUseCase _useCase;

    public AtualizarFotoPerfilUseCaseTests()
    {
        _useCase = new AtualizarFotoPerfilUseCase(_usuarioRepositorio, _armazenamento, _usuarioRepositorio, NullLogger<AtualizarFotoPerfilUseCase>.Instance);
    }

    [Fact(DisplayName = "Salva a foto no armazenamento e a chave no usuário")]
    public async Task ExecuteAsync_DeveSalvarNoArmazenamentoEAChaveNoUsuario()
    {
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario { Id = usuarioId, Nome = "Teste", Email = "teste@exemplo.com" };
        _usuarioRepositorio.Usuarios.Add(usuario);
        var bytes = new byte[] { 1, 2, 3 };

        var command = new AtualizarFotoPerfilCommand(usuarioId, usuarioId, new MemoryStream(bytes), "image/jpeg", bytes.Length);
        await _useCase.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(usuarioId.ToString(), usuario.FotoChave);
        Assert.Equal(bytes, _armazenamento.Objetos[usuario.FotoChave!].Conteudo);
        Assert.Equal("image/jpeg", _armazenamento.Objetos[usuario.FotoChave!].ContentType);
    }

    [Fact(DisplayName = "Tipo de arquivo não aceito lança FotoPerfilInvalidaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoTipoInvalido()
    {
        var usuarioId = Guid.NewGuid();
        _usuarioRepositorio.Usuarios.Add(new Usuario { Id = usuarioId, Nome = "Teste", Email = "teste@exemplo.com" });
        var conteudo = new MemoryStream([1, 2, 3]);

        var command = new AtualizarFotoPerfilCommand(usuarioId, usuarioId, conteudo, "application/pdf", conteudo.Length);

        await Assert.ThrowsAsync<FotoPerfilInvalidaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Arquivo maior que 5 MB lança FotoPerfilInvalidaException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoArquivoMuitoGrande()
    {
        var usuarioId = Guid.NewGuid();
        _usuarioRepositorio.Usuarios.Add(new Usuario { Id = usuarioId, Nome = "Teste", Email = "teste@exemplo.com" });
        var conteudo = new MemoryStream([1, 2, 3]);

        var command = new AtualizarFotoPerfilCommand(usuarioId, usuarioId, conteudo, "image/jpeg", 6 * 1024 * 1024);

        await Assert.ThrowsAsync<FotoPerfilInvalidaException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }

    [Fact(DisplayName = "Usuário autenticado tentando atualizar foto de outro usuário lança PerfilNaoEncontradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoIdsDivergem()
    {
        var meuId = Guid.NewGuid();
        var idDeOutroUsuario = Guid.NewGuid();
        var conteudo = new MemoryStream([1, 2, 3]);

        var command = new AtualizarFotoPerfilCommand(meuId, idDeOutroUsuario, conteudo, "image/jpeg", conteudo.Length);

        await Assert.ThrowsAsync<PerfilNaoEncontradoException>(() => _useCase.ExecuteAsync(command, CancellationToken.None));
    }
}
