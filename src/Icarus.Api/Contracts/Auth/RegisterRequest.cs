namespace Icarus.Api.Contracts.Auth;

/// <summary>Corpo da requisição de cadastro (RF-01). Contrato HTTP, desacoplado do comando do caso de uso.</summary>
public sealed record RegisterRequest(string Nome, string Email, string Senha, DateOnly DataNascimento);
