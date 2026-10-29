using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarCampanha;

/// <summary>
/// Não recebe AreaDaVida: é copiada do épico (RF-03). <see cref="PrazoInicio"/> nulo assume
/// hoje; <see cref="PrazoFim"/> nulo assume o prazo fim do épico.
/// </summary>
public sealed record CriarCampanhaCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    long EpicoId,
    string Titulo,
    string? Descricao,
    Dificuldade Dificuldade,
    DateOnly? PrazoInicio,
    DateOnly? PrazoFim,
    decimal? ValorMensurado,
    string? Unidade);
