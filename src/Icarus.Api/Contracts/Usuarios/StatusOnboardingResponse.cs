using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Usuarios;

/// <summary>
/// <see cref="Epico"/> só é preenchido quando <see cref="Status"/> é <c>Concluido</c>;
/// <see cref="Erro"/> só quando é <c>Falhou</c>.
/// </summary>
public sealed record StatusOnboardingResponse(
    StatusTrabalhoOnboarding Status,
    EpicoOnboardingResponse? Epico,
    string? Erro);

public sealed record EpicoOnboardingResponse(
    long Id,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    EstadoEpico Estado,
    CampanhaOnboardingResponse Campanha);

public sealed record CampanhaOnboardingResponse(
    long Id,
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
    List<MissaoOnboardingResponse> Missoes);

public sealed record MissaoOnboardingResponse(
    long Id,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly? DataLimite,
    DateTimeOffset Horario,
    StatusMissao Status);
