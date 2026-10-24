using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ListarMissoesAvulsas;

public sealed record MissaoAvulsaListadaResult(
    long Id,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly? DataLimite,
    DateTimeOffset Horario,
    StatusMissao Status,
    DateTimeOffset? ConcluidaEm,
    bool Recorrente);
