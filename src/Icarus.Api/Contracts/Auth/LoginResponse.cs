namespace Icarus.Api.Contracts.Auth;

/// <summary>Corpo da resposta de login (RF-02.1).</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset AccessTokenExpiraEm, string RefreshToken, DateTimeOffset RefreshTokenExpiraEm);
