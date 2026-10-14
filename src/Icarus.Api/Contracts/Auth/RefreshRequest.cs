namespace Icarus.Api.Contracts.Auth;

/// <summary>Corpo da requisição de refresh (RF-02). Contrato HTTP, desacoplado do comando do caso de uso.</summary>
public sealed record RefreshRequest(string RefreshToken);
