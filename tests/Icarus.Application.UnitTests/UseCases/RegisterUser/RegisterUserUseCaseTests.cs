using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.RegisterUser;
using Icarus.Domain.Entities;

namespace Icarus.Application.UnitTests.UseCases.RegisterUser;

/// <summary>
/// Fake em memória — evita precisar de um Postgres real só para testar a regra do caso de uso.
/// Fica aqui, e não como classe pública reaproveitável, porque cada caso de uso tende a precisar
/// de um comportamento de fake ligeiramente diferente.
/// </summary>
internal sealed class UsuarioRepositoryEmMemoria : IUsuarioRepository, IUnitOfWork
{
    public List<Usuario> Usuarios { get; } = [];

    public Task<bool> ExisteComEmailAsync(string email, CancellationToken cancellationToken)
        => Task.FromResult(Usuarios.Any(u => u.Email == email));

    public Task AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        Usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Fake sem custo computacional — o Argon2id de verdade (Icarus.Infrastructure.Security.PasswordHasher)
/// não pertence a um teste de Icarus.Application: essa camada não pode depender da Infrastructure
/// (inverteria a Clean Architecture), e testar a regra do caso de uso não exige um hash real.
/// O algoritmo em si é testado à parte, em Icarus.Infrastructure.UnitTests.
/// </summary>
internal sealed class PasswordHasherFake : IPasswordHasher
{
    public string Hash(string senha) => $"hash-fake:{senha}";

    public bool Verify(string senha, string hash) => hash == Hash(senha);
}

public class RegisterUserUseCaseTests
{
    private readonly UsuarioRepositoryEmMemoria _repositorio = new();
    private readonly RegisterUserUseCase _useCase;

    public RegisterUserUseCaseTests()
    {
        _useCase = new RegisterUserUseCase(_repositorio, _repositorio, new PasswordHasherFake());
    }

    [Fact(DisplayName = "Cadastro bem-sucedido normaliza o e-mail e nunca guarda a senha em texto puro")]
    public async Task ExecuteAsync_DeveCadastrarComEmailNormalizadoESenhaComHash()
    {
        var comando = new RegisterUserCommand("Ana Silva", "  Ana@Exemplo.com ", "minhaSenha123", new DateOnly(2000, 1, 1));

        var resultado = await _useCase.ExecuteAsync(comando, CancellationToken.None);

        var usuario = Assert.Single(_repositorio.Usuarios);
        Assert.Equal(resultado.UsuarioId, usuario.Id);
        Assert.Equal("ana@exemplo.com", usuario.Email);
        Assert.NotEqual("minhaSenha123", usuario.SenhaHash);
    }

    [Fact(DisplayName = "Cadastro com e-mail já existente lança EmailJaCadastradoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoEmailJaExiste()
    {
        var primeiroComando = new RegisterUserCommand("Ana Silva", "ana@exemplo.com", "minhaSenha123", new DateOnly(2000, 1, 1));
        await _useCase.ExecuteAsync(primeiroComando, CancellationToken.None);

        var segundoComando = new RegisterUserCommand("Outra Ana", "ANA@EXEMPLO.COM", "outraSenha456", new DateOnly(1999, 5, 5));

        await Assert.ThrowsAsync<EmailJaCadastradoException>(
            () => _useCase.ExecuteAsync(segundoComando, CancellationToken.None));
    }
}
