namespace Icarus.Api.Contracts.Dev;

/// <summary>Resposta do endpoint de desenvolvimento que gera um access token de teste.</summary>
public sealed record DevTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiraEm);
