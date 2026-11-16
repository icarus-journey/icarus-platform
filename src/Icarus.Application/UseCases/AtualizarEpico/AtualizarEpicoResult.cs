using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.AtualizarEpico;

public sealed record AtualizarEpicoResult(
    long Id,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    EstadoEpico Estado);
