namespace Icarus.Application.UseCases.Login;

/// <summary>Saída do caso de uso de login (RF-02.1).</summary>
public sealed record LoginResult(string AccessToken, DateTimeOffset AccessTokenExpiraEm, string RefreshToken, DateTimeOffset RefreshTokenExpiraEm);
