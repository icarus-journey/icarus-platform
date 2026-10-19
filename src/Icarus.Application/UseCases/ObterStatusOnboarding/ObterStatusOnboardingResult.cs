using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ObterStatusOnboarding;

/// <summary>
/// Saída do caso de uso de consultar onboarding. <see cref="Epico"/> só é preenchido quando
/// <see cref="Status"/> é <see cref="StatusTrabalhoOnboarding.Concluido"/>; <see cref="ErroMensagem"/>
/// só quando é <see cref="StatusTrabalhoOnboarding.Falhou"/>.
/// </summary>
public sealed record ObterStatusOnboardingResult(
    StatusTrabalhoOnboarding Status,
    EpicoOnboardingResult? Epico,
    string? ErroMensagem);

public sealed record EpicoOnboardingResult(
    long Id,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    EstadoEpico Estado,
    CampanhaOnboardingResult Campanha);

public sealed record CampanhaOnboardingResult(
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
    IReadOnlyList<MissaoOnboardingResult> Missoes);

public sealed record MissaoOnboardingResult(
    long Id,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    int Pontuacao,
    DateOnly? DataLimite,
    DateTimeOffset Horario,
    StatusMissao Status);
