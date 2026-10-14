namespace Icarus.Application.UseCases.ObterPerfil;

/// <summary>
/// Saída do caso de uso de obter perfil. Só os campos do cadastro (RF-01) — nenhum
/// campo de outro RF (ex.: saldo de pontos é RF-05/RF-06, ainda não implementado).
/// </summary>
public sealed record ObterPerfilResult(Guid Id, string Nome, string Email, DateOnly DataNascimento);
