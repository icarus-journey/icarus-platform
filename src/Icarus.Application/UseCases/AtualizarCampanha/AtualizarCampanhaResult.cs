using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.AtualizarCampanha;

public sealed record AtualizarCampanhaResult(
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
