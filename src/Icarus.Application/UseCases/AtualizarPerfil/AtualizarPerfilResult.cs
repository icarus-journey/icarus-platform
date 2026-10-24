namespace Icarus.Application.UseCases.AtualizarPerfil;

public sealed record AtualizarPerfilResult(Guid Id, string Nome, string Email, DateOnly DataNascimento,
    bool? Estuda, decimal? CargaHorariaEstudoSemanal, bool? Trabalha,
    decimal? CargaHorariaTrabalhoSemanal, string? FusoHorario, bool TemFoto);
