using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.Logout;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icarus.Application.UnitTests.UseCases.Logout;

/// <summary>
/// Fake em memória — evita precisar de um Postgres real só para testar a regra do caso de uso.
/// Fica aqui, e não como classe pública reaproveitável, mesmo racional dos outros casos de uso.
/// </summary>
internal sealed class TokenRenovacaoRepositorioEmMemoriaParaLogout : ITokenRenovacaoRepository, IUnitOfWork
{
    public List<TokenRenovacao> Tokens { get; } = [];
    public int ChamadasDeSalvar { get; private set; }

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
        ChamadasDeSalvar++;
        return Task.CompletedTask;
    }
}

/// <summary>Fake sem criptografia real — ver o mesmo comentário em LoginUseCaseTests.</summary>
internal sealed class JwtTokenGeneratorFakeParaLogout : IJwtTokenGenerator
{
    public (string Token, DateTimeOffset ExpiraEm) GerarAccessToken(Guid usuarioId)
        => throw new NotSupportedException("Logout não gera access token.");

    public (string Token, string TokenHash, DateTimeOffset ExpiraEm) GerarRefreshToken()
        => throw new NotSupportedException("Logout não gera refresh token.");

    public string CalcularHashDoRefreshToken(string refreshToken) => $"hash-fake:{refreshToken}";
}

public class LogoutUseCaseTests
{
    private readonly TokenRenovacaoRepositorioEmMemoriaParaLogout _repositorio = new();
    private readonly LogoutUseCase _useCase;

    public LogoutUseCaseTests()
    {
        _useCase = new LogoutUseCase(_repositorio, _repositorio, new JwtTokenGeneratorFakeParaLogout(), NullLogger<LogoutUseCase>.Instance);
    }

    [Fact(DisplayName = "Logout com token válido marca RevogadoEm e salva")]
    public async Task ExecuteAsync_DeveRevogarTokenValido()
    {
        var usuarioId = Guid.NewGuid();
        _repositorio.Tokens.Add(new TokenRenovacao
        {
            Id = 1,
            UsuarioId = usuarioId,
            TokenHash = "hash-fake:meu-refresh-token",
            ExpiraEm = DateTimeOffset.UtcNow.AddDays(7)
        });

        await _useCase.ExecuteAsync(new LogoutCommand("meu-refresh-token"), CancellationToken.None);

        Assert.NotNull(_repositorio.Tokens[0].RevogadoEm);
        Assert.Equal(1, _repositorio.ChamadasDeSalvar);
    }

    [Fact(DisplayName = "Logout com token inexistente não lança exceção e não salva nada")]
    public async Task ExecuteAsync_NaoDeveLancarExcecaoQuandoTokenNaoExiste()
    {
        await _useCase.ExecuteAsync(new LogoutCommand("token-que-nao-existe"), CancellationToken.None);

        Assert.Equal(0, _repositorio.ChamadasDeSalvar);
    }

    [Fact(DisplayName = "Logout com token já revogado não lança exceção e não salva de novo")]
    public async Task ExecuteAsync_NaoDeveLancarExcecaoQuandoTokenJaRevogado()
    {
        _repositorio.Tokens.Add(new TokenRenovacao
        {
            Id = 1,
            UsuarioId = Guid.NewGuid(),
            TokenHash = "hash-fake:token-revogado",
            ExpiraEm = DateTimeOffset.UtcNow.AddDays(7),
            RevogadoEm = DateTimeOffset.UtcNow.AddMinutes(-5)
        });

        await _useCase.ExecuteAsync(new LogoutCommand("token-revogado"), CancellationToken.None);

        Assert.Equal(0, _repositorio.ChamadasDeSalvar);
    }
}
