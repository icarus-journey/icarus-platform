namespace Icarus.Application.UseCases.ObterPerfil;

/// <summary>
/// Dados cadastrais, contexto analítico opcional e fuso do titular (RF-01).
/// </summary>
public sealed record ObterPerfilResult(Guid Id, string Nome, string Email, DateOnly DataNascimento,
    bool? Estuda = null, decimal? CargaHorariaEstudoSemanal = null,
    bool? Trabalha = null, decimal? CargaHorariaTrabalhoSemanal = null, string? FusoHorario = null);
