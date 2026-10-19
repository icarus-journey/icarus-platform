using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

/// <summary>
/// CampanhaId nulo cria uma missão avulsa (RF-03). Horario no formato "HH:mm"; omitido assume
/// 09:00. Recorrência não é suportada por este endpoint (RF-04, fora de escopo aqui).
/// </summary>
public sealed record CriarMissaoRequest(
    long? CampanhaId,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    DateOnly? DataLimite,
    string? Horario);
