using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Epicos;

/// <summary>Épico com campanhas e missões aninhadas — usado na listagem (RF-01.5).</summary>
public sealed record EpicoComCampanhasResponse(
    long Id,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    EstadoEpico Estado,
    List<CampanhaComMissoesResponse> Campanhas);

public sealed record CampanhaComMissoesResponse(
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
    EstadoCampanha Estado,
    List<MissaoResumoResponse> Missoes);

public sealed record MissaoResumoResponse(
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
