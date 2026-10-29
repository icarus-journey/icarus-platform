namespace Icarus.Api.Contracts.Usuarios;

/// <summary>
/// Perfil próprio: cadastro imutável, contexto opcional e fuso (RF-01). <see cref="TemFoto"/>
/// diz se existe foto pra buscar em <c>GET .../foto</c> (o conteúdo da imagem não vem aqui).
/// </summary>
public sealed record UsuarioResponse(Guid Id, string Nome, string Email, DateOnly DataNascimento,
    bool? Estuda, decimal? CargaHorariaEstudoSemanal, bool? Trabalha,
    decimal? CargaHorariaTrabalhoSemanal, string? FusoHorario, bool TemFoto);
