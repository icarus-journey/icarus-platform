namespace Icarus.Application.UseCases.RegisterUser;

/// <summary>Dados de entrada do cadastro (RF-01).</summary>
public sealed record RegisterUserCommand(string Nome, string Email, string Senha, DateOnly DataNascimento);
