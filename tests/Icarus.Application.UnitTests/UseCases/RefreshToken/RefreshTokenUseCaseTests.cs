using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.RefreshToken;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.RefreshToken;

/// <summary>
/// Fake em memória — evita precisar de um Postgres real só para testar a regra do caso de uso.
/// Fica aqui, e não como classe pública reaproveitável, mesmo racional dos outros casos de uso.
/// </summary>
internal sealed class TokenRenovacaoRepositorioEmMemoria : ITokenRenovacaoRepository, IUnitOfWork
{
    public List<TokenRenovacao> Tokens { get; } = [];

    public Task AdicionarAsync(TokenRenovacao tokenRenovacao, CancellationToken cancellationToken)
    {
        Tokens.Add(tokenRenovacao);
        return Task.CompletedTask;
    }

    public Task<TokenRenovacao?> BuscarPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        => Task.FromResult(Tokens.SingleOrDefault(t => t.TokenHash == tokenHash));

    public Task RevogarTodosDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        foreach (var token in Tokens.Where(t => t.UsuarioId == usuarioId && t.RevogadoEm is null))
        {
            token.RevogadoEm = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        // Simula o que o EF Core faria ao persistir a relação SubstituidoPor: dá ao
        // token novo um Id e resolve o SubstituidoPorId do token antigo a partir dela.
        var proximoId = Tokens.Count == 0 ? 1 : Tokens.Max(t => t.Id) + 1;
        foreach (var token in Tokens.Where(t => t.Id == 0))
        {
            token.Id = proximoId++;
        }

        foreach (var token in Tokens.Where(t => t.SubstituidoPor is not null && t.SubstituidoPorId is null))
        {
            token.SubstituidoPorId = token.SubstituidoPor!.Id;
        }

        return Task.CompletedTask;
    }
}

/// <summary>Fake sem criptografia real — ver o mesmo comentário em LoginUseCaseTests.</summary>
internal sealed class JwtTokenGeneratorFakeParaRefresh : IJwtTokenGenerator
{
    public int ChamadasDeGerarRefreshToken { get; private set; }

    public (string Token, DateTimeOffset ExpiraEm) GerarAccessToken(Guid usuarioId)
        => ($"access-token-fake:{usuarioId}", DateTimeOffset.UtcNow.AddMinutes(15));

    public (string Token, string TokenHash, DateTimeOffset ExpiraEm) GerarRefreshToken()
    {
        ChamadasDeGerarRefreshToken++;
        return ($"refresh-token-fake-{ChamadasDeGerarRefreshToken}", $"refresh-token-fake-hash-{ChamadasDeGerarRefreshToken}", DateTimeOffset.UtcNow.AddDays(7));
    }

    public string CalcularHashDoRefreshToken(string refreshToken) => $"hash-fake:{refreshToken}";
}

public class RefreshTokenUseCaseTests
{
    private readonly TokenRenovacaoRepositorioEmMemoria _repositorio = new();
    private readonly JwtTokenGeneratorFakeParaRefresh _jwtTokenGenerator = new();
    private readonly RefreshTokenUseCase _useCase;
    private readonly Guid _usuarioId = Guid.NewGuid();

    public RefreshTokenUseCaseTests()
    {
        _useCase = new RefreshTokenUseCase(_repositorio, _repositorio, _jwtTokenGenerator, NullLogger<RefreshTokenUseCase>.Instance);
    }

    private TokenRenovacao AdicionarTokenValido(string tokenHash, Guid? usuarioId = null)
    {
        var token = new TokenRenovacao
        {
            Id = _repositorio.Tokens.Count + 1,
            UsuarioId = usuarioId ?? _usuarioId,
            TokenHash = tokenHash,
            ExpiraEm = DateTimeOffset.UtcNow.AddDays(7)
        };
        _repositorio.Tokens.Add(token);
        return token;
    }

    [Fact(DisplayName = "Refresh válido gera um par novo de tokens e marca o antigo como substituído")]
    public async Task ExecuteAsync_DeveRotacionarOToken()
    {
        var tokenAntigo = AdicionarTokenValido("hash-fake:token-original");
        var comando = new RefreshTokenCommand("token-original");

        var resultado = await _useCase.ExecuteAsync(comando, CancellationToken.None);

        Assert.Equal("refresh-token-fake-1", resultado.RefreshToken);
        Assert.NotNull(tokenAntigo.SubstituidoPorId);
        Assert.Equal(2, _repositorio.Tokens.Count);
    }

    [Fact(DisplayName = "Refresh com token inexistente lança RefreshTokenInvalidoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoTokenNaoExiste()
    {
        var comando = new RefreshTokenCommand("token-que-nao-existe");

        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(
            () => _useCase.ExecuteAsync(comando, CancellationToken.None));
    }

    [Fact(DisplayName = "Refresh com token expirado lança RefreshTokenInvalidoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoTokenExpirado()
    {
        var token = AdicionarTokenValido("hash-fake:token-expirado");
        token.ExpiraEm = DateTimeOffset.UtcNow.AddDays(-1);
        var comando = new RefreshTokenCommand("token-expirado");

        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(
            () => _useCase.ExecuteAsync(comando, CancellationToken.None));
    }

    [Fact(DisplayName = "Refresh com token revogado lança RefreshTokenInvalidoException")]
    public async Task ExecuteAsync_DeveLancarExcecaoQuandoTokenRevogado()
    {
        var token = AdicionarTokenValido("hash-fake:token-revogado");
        token.RevogadoEm = DateTimeOffset.UtcNow;
        var comando = new RefreshTokenCommand("token-revogado");

        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(
            () => _useCase.ExecuteAsync(comando, CancellationToken.None));
    }

    [Fact(DisplayName = "Reuso de token já rotacionado lança a exceção e revoga todos os tokens ativos do usuário")]
    public async Task ExecuteAsync_DeveRevogarTudoQuandoDetectaReuso()
    {
        var tokenJaRotacionado = AdicionarTokenValido("hash-fake:token-reusado", _usuarioId);
        var outroTokenAtivo = AdicionarTokenValido("hash-fake:outro-token-ativo", _usuarioId);
        tokenJaRotacionado.SubstituidoPorId = 999; // já foi trocado por outro antes

        var comando = new RefreshTokenCommand("token-reusado");

        await Assert.ThrowsAsync<RefreshTokenInvalidoException>(
            () => _useCase.ExecuteAsync(comando, CancellationToken.None));

        Assert.NotNull(outroTokenAtivo.RevogadoEm);
    }
}
