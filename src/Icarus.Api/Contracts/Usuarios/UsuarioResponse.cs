namespace Icarus.Api.Contracts.Usuarios;

/// <summary>
/// Corpo da resposta do perfil (RF-01). Campos do cadastro — ver PROJETO.md, seção 4.4 — mais
/// <see cref="TemFoto"/>, que diz se existe foto pra buscar em <c>GET .../foto</c> (o próprio
/// conteúdo da imagem não vem aqui).
/// </summary>
public sealed record UsuarioResponse(Guid Id, string Nome, string Email, DateOnly DataNascimento, bool TemFoto);
