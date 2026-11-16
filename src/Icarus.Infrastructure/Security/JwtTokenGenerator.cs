using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Icarus.Infrastructure.Security;

/// <summary>
/// Gera o access token (JWT) e o refresh token do RF-02. Usa
/// <see cref="JsonWebTokenHandler"/> — mais rápido que o antigo
/// JwtSecurityTokenHandler e o mesmo handler que o
/// Microsoft.AspNetCore.Authentication.JwtBearer já usa por padrão para
/// validar tokens desde o .NET 8, mantendo geração e validação consistentes.
/// </summary>
public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    // Divergência deliberada do RF-02 (JWT de 24h sem renovação): access token
    // curto + refresh token longo com rotação. Ver PROJETO.md, seção 4.4.
    private static readonly TimeSpan DuracaoAccessToken = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DuracaoRefreshToken = TimeSpan.FromDays(7);

    private const int TamanhoRefreshTokenEmBytes = 32;

    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiraEm) GerarAccessToken(Guid usuarioId)
    {
        var expiraEm = DateTimeOffset.UtcNow.Add(DuracaoAccessToken);

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ChaveSecreta));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        // Só o Id do usuário na claim — minimiza dado pessoal exposto no token
        // (o JWT não é criptografado, só assinado; qualquer um pode decodificar o payload).
        var descritor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())]),
            Issuer = _options.Emissor,
            Audience = _options.Audiencia,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = credenciais
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descritor);

        return (token, expiraEm);
    }

    public (string Token, string TokenHash, DateTimeOffset ExpiraEm) GerarRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TamanhoRefreshTokenEmBytes));
        var hash = CalcularHashDoRefreshToken(token);
        var expiraEm = DateTimeOffset.UtcNow.Add(DuracaoRefreshToken);

        return (token, hash, expiraEm);
    }

    public string CalcularHashDoRefreshToken(string refreshToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
