namespace Icarus.Application.UseCases.RefreshToken;

/// <summary>Saída do caso de uso de refresh (RF-02). Mesmo formato do login — um novo par de tokens.</summary>
public sealed record RefreshTokenResult(string AccessToken, DateTimeOffset AccessTokenExpiraEm, string RefreshToken, DateTimeOffset RefreshTokenExpiraEm);
