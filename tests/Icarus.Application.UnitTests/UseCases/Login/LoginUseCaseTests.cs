using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.Login;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.Login;

/// <summary>
/// Fake em memória — evita precisar de um Postgres real só para testar a regra do caso de uso.
/// Fica aqui, e não como classe pública reaproveitável, porque cada caso de uso tende a precisar
/// de um comportamento de fake ligeiramente diferente.
/// </summary>
internal sealed class UsuarioRepositorioEmMemoriaParaLogin : IUsuarioRepository, ITokenRenovacaoRepository, IUnitOfWork
{
    public List<Usuario> Usuarios { get; } = [];
    public List<TokenRenovacao> TokensRenovacao { get; } = [];

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

    public Task AdicionarAsync(TokenRenovacao tokenRenovacao, CancellationToken cancellationToken)
    {
        TokensRenovacao.Add(tokenRenovacao);
        return Task.CompletedTask;
    }

    public Task<TokenRenovacao?> BuscarPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => Task.FromResult(TokensRenovacao.SingleOrDefault(t => t.TokenHash == tokenHash));

    public Task RevogarTodosDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        foreach (var token in TokensRenovacao.Where(t => t.UsuarioId == usuarioId && t.RevogadoEm is null))
        {
            token.RevogadoEm = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Fake sem custo computacional — ver o mesmo comentário em RegisterUserUseCaseTests.</summary>
internal sealed class PasswordHasherFakeParaLogin : IPasswordHasher
{
    public string Hash(string senha) => $"hash-fake:{senha}";

    public bool Verify(string senha, string hash) => hash == Hash(senha);
}

/// <summary>Fake sem criptografia real — testar o caso de uso não exige um JWT de verdade.</summary>
internal sealed class JwtTokenGeneratorFake : IJwtTokenGenerator
{
    public (string Token, DateTimeOffset ExpiraEm) GerarAccessToken(Guid usuarioId)
        => ($"access-token-fake:{usuarioId}", DateTimeOffset.UtcNow.AddMinutes(15));

    public (string Token, string TokenHash, DateTimeOffset ExpiraEm) GerarRefreshToken()
        => ("refresh-token-fake", "refresh-token-fake-hash", DateTimeOffset.UtcNow.AddDays(7));

    public string CalcularHashDoRefreshToken(string refreshToken) => $"hash-fake:{refreshToken}";
}

/// <summary>Fake em memória — mesmo comportamento da implementação real, sem depender de IMemoryCache.</summary>
internal sealed class LoginAttemptLimiterFake : ILoginAttemptLimiter
{
    private readonly Dictionary<(string Email, string Origem), int> _falhas = [];

    public bool EstaBloqueado(string email, string origem)
        => _falhas.TryGetValue((email, origem), out var quantidade) && quantidade >= 5;

    public void RegistrarFalha(string email, string origem)
        => _falhas[(email, origem)] = _falhas.GetValueOrDefault((email, origem)) + 1;

    public void RegistrarSucesso(string email, string origem)
        => _falhas.Remove((email, origem));
}

public class LoginUseCaseTests
{
    private const string Origem = "127.0.0.1";

    private readonly UsuarioRepositorioEmMemoriaParaLogin _repositorio = new();
    private readonly PasswordHasherFakeParaLogin _passwordHasher = new();
    private readonly LoginAttemptLimiterFake _loginAttemptLimiter = new();
    private readonly LoginUseCase _useCase;

    public LoginUseCaseTests()
    {
        _useCase = new LoginUseCase(
            _repositorio,
            _repositorio,
            _repositorio,
            _passwordHasher,
            new JwtTokenGeneratorFake(),
            _loginAttemptLimiter,
            NullLogger<LoginUseCase>.Instance);
    }

    private void AdicionarUsuario(string email, string senha)
    {
        _repositorio.Usuarios.Add(new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = "Ana Silva",
            Email = email,
            SenhaHash = _passwordHasher.Hash(senha),
            DataNascimento = new DateOnly(2000, 1, 1)
        });
    }

    [Fact(DisplayName = "Login com credenciais válidas gera os dois tokens e persiste o refresh token")]
    public async Task ExecuteAsync_DeveAutenticarEGerarTokens()
    {
        AdicionarUsuario("ana@exemplo.com", "minhaSenha123");
        var comando = new LoginCommand("ana@exemplo.com", "minhaSenha123", Origem);

        var resultado = await _useCase.ExecuteAsync(comando, CancellationToken.None);

        Assert.Equal("refresh-token-fake", resultado.RefreshToken);
        var tokenPersistido = Assert.Single(_repositorio.TokensRenovacao);
        Assert.Equal("refresh-token-fake-hash", tokenPersistido.TokenHash);
        Assert.Equal(_repositorio.Usuarios[0].Id, tokenPersistido.UsuarioId);
    }

    [Fact(DisplayName = "Login com e-mail inexistente lança CredenciaisInvalidasException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoEmailNaoExiste()
    {
        var comando = new LoginCommand("inexistente@exemplo.com", "qualquerSenha", Origem);

        await Assert.ThrowsAsync<CredenciaisInvalidasException>(
            () => _useCase.ExecuteAsync(comando, CancellationToken.None));
    }

    [Fact(DisplayName = "Login com senha incorreta lança CredenciaisInvalidasException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoSenhaIncorreta()
    {
        AdicionarUsuario("ana@exemplo.com", "minhaSenha123");
        var comando = new LoginCommand("ana@exemplo.com", "senhaErrada", Origem);

        await Assert.ThrowsAsync<CredenciaisInvalidasException>(
            () => _useCase.ExecuteAsync(comando, CancellationToken.None));
    }

    [Fact(DisplayName = "Após 5 falhas para o mesmo e-mail + origem, a 6ª tentativa lança LimiteDeTentativasExcedidoException sem checar a senha")]
    public async Task ExecuteAsync_DeveBloquearAposCincoFalhas()
    {
        AdicionarUsuario("ana@exemplo.com", "minhaSenha123");
        var comandoErrado = new LoginCommand("ana@exemplo.com", "senhaErrada", Origem);

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<CredenciaisInvalidasException>(
                () => _useCase.ExecuteAsync(comandoErrado, CancellationToken.None));
        }

        // Mesmo com a senha CERTA, a 6ª tentativa é bloqueada pelo limite.
        var comandoCerto = new LoginCommand("ana@exemplo.com", "minhaSenha123", Origem);
        await Assert.ThrowsAsync<LimiteDeTentativasExcedidoException>(
            () => _useCase.ExecuteAsync(comandoCerto, CancellationToken.None));
    }

    [Fact(DisplayName = "Login bem-sucedido zera o contador de falhas anteriores")]
    public async Task ExecuteAsync_DeveZerarContadorAposSucesso()
    {
        AdicionarUsuario("ana@exemplo.com", "minhaSenha123");
        var comandoErrado = new LoginCommand("ana@exemplo.com", "senhaErrada", Origem);
        var comandoCerto = new LoginCommand("ana@exemplo.com", "minhaSenha123", Origem);

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<CredenciaisInvalidasException>(
                () => _useCase.ExecuteAsync(comandoErrado, CancellationToken.None));
        }

        await _useCase.ExecuteAsync(comandoCerto, CancellationToken.None); // zera o contador

        // Mais 4 falhas depois do sucesso não deveriam bloquear (contador zerado).
        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<CredenciaisInvalidasException>(
                () => _useCase.ExecuteAsync(comandoErrado, CancellationToken.None));
        }

        await _useCase.ExecuteAsync(comandoCerto, CancellationToken.None);
    }
}
