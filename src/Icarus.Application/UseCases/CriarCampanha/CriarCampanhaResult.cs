using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarCampanha;

public sealed record CriarCampanhaResult(
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
