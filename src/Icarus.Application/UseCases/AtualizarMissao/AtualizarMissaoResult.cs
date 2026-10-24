using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.AtualizarMissao;

public sealed record AtualizarMissaoResult(
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
    DateTimeOffset? ConcluidaEm,
    bool Recorrente);
