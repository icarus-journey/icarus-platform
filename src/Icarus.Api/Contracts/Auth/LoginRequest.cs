namespace Icarus.Api.Contracts.Auth;

/// <summary>Corpo da requisição de login (RF-02.1). Contrato HTTP, desacoplado do comando do caso de uso.</summary>
public sealed record LoginRequest(string Email, string Senha);
