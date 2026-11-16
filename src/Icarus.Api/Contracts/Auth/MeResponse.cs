namespace Icarus.Api.Contracts.Auth;

/// <summary>
/// Resposta do endpoint de exemplo protegido (<c>GET /api/auth/me</c>). Não é o
/// `GET` de perfil completo (RF-01, ainda deferido) — só prova que o access
/// token é exigido e que o `UsuarioId` lido do token é o correto.
/// </summary>
public sealed record MeResponse(Guid UsuarioId);
