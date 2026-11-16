using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Epicos;

public sealed record CriarEpicoRequest(
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    DateOnly? PrazoInicio,
    DateOnly? PrazoFim);
