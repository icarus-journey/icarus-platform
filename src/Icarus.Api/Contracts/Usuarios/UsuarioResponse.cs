namespace Icarus.Api.Contracts.Usuarios;

/// <summary>Perfil próprio: cadastro imutável, contexto opcional e fuso (RF-01).</summary>
public sealed record UsuarioResponse(Guid Id, string Nome, string Email, DateOnly DataNascimento,
    bool? Estuda, decimal? CargaHorariaEstudoSemanal, bool? Trabalha,
    decimal? CargaHorariaTrabalhoSemanal, string? FusoHorario);
