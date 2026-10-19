using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarEpico;

public sealed record CriarEpicoResult(
    long Id,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    EstadoEpico Estado);
