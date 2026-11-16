using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Campanhas;

public sealed record CampanhaResponse(
    long Id,
    long EpicoId,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    decimal? ValorMensurado,
    string? Unidade,
    decimal? ValorAtual,
    EstadoCampanha Estado);
