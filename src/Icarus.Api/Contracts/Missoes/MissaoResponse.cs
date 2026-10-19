using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

public sealed record MissaoResponse(
    long Id,
    long? CampanhaId,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly? DataLimite,
    DateTimeOffset Horario,
    StatusMissao Status,
    DateTimeOffset? ConcluidaEm);
