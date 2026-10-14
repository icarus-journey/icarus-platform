namespace Icarus.Api.Contracts.Usuarios;

/// <summary>Corpo da resposta do perfil (RF-01). Só os campos do cadastro — ver PROJETO.md, seção 4.4.</summary>
public sealed record UsuarioResponse(Guid Id, string Nome, string Email, DateOnly DataNascimento);
