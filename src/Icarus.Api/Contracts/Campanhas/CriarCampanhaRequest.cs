using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Campanhas;

/// <summary>Sem AreaDaVida: copiada do épico ao criar (RF-03).</summary>
public sealed record CriarCampanhaRequest(
    long EpicoId,
    string Titulo,
    string? Descricao,
    Dificuldade Dificuldade,
    DateOnly? PrazoInicio,
    DateOnly? PrazoFim,
    decimal? ValorMensurado,
    string? Unidade);
