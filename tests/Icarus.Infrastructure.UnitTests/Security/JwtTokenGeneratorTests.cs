using System.Security.Cryptography;
using System.Text;
using Icarus.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Icarus.Infrastructure.UnitTests.Security;

public class JwtTokenGeneratorTests
{
    private readonly JwtOptions _options = new()
    {
        ChaveSecreta = "chave-secreta-de-teste-com-pelo-menos-32-bytes!!",
        Emissor = "icarus-platform-teste",
        Audiencia = "icarus-mobile-teste"
    };

    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        _generator = new JwtTokenGenerator(Options.Create(_options));
    }

    [Fact(DisplayName = "GerarAccessToken produz um JWT válido, assinado com a mesma chave e contendo o Id do usuário")]
    public async Task GerarAccessToken_DeveProduzirTokenValidoComIdDoUsuario()
    {
        var usuarioId = Guid.NewGuid();

        var (token, expiraEm) = _generator.GerarAccessToken(usuarioId);

        var handler = new JsonWebTokenHandler();
        var resultado = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = _options.Emissor,
            ValidAudience = _options.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ChaveSecreta))
        });

        Assert.True(resultado.IsValid, resultado.Exception?.ToString());
        var jwt = Assert.IsType<JsonWebToken>(resultado.SecurityToken);
        Assert.Equal(usuarioId.ToString(), jwt.GetClaim(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        Assert.True(expiraEm > DateTimeOffset.UtcNow && expiraEm <= DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Fact(DisplayName = "GerarRefreshToken devolve o token em texto puro e separadamente o hash SHA-256 dele")]
    public void GerarRefreshToken_DeveDevolverTokenEHashCorrespondente()
    {
        var (token, tokenHash, expiraEm) = _generator.GerarRefreshToken();

        var hashEsperado = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        Assert.Equal(hashEsperado, tokenHash);
        Assert.True(expiraEm > DateTimeOffset.UtcNow.AddDays(6) && expiraEm <= DateTimeOffset.UtcNow.AddDays(7));
    }

    [Fact(DisplayName = "GerarRefreshToken gera um valor diferente a cada chamada")]
    public void GerarRefreshToken_DeveGerarValoresDiferentes()
    {
        var (token1, _, _) = _generator.GerarRefreshToken();
        var (token2, _, _) = _generator.GerarRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact(DisplayName = "CalcularHashDoRefreshToken produz o mesmo hash que GerarRefreshToken gerou pro mesmo token")]
    public void CalcularHashDoRefreshToken_DeveBaterComOHashDeGerarRefreshToken()
    {
        var (token, tokenHash, _) = _generator.GerarRefreshToken();

        var hashRecalculado = _generator.CalcularHashDoRefreshToken(token);

        Assert.Equal(tokenHash, hashRecalculado);
    }
}
