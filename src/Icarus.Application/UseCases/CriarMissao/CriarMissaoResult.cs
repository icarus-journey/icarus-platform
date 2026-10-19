using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarMissao;

public sealed record CriarMissaoResult(
    long Id,
    long? CampanhaId,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly? DataLimite,
    DateTimeOffset Horario,
    StatusMissao Status);
