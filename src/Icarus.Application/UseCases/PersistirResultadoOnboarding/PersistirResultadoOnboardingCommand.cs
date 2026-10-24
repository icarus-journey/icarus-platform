using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.PersistirResultadoOnboarding;

/// <summary>
/// Entrada do caso de uso que persiste o resultado assíncrono do worker de IA. Contrato
/// interno, desacoplado da mensageria — o mapeamento da mensagem RabbitMQ pra este comando
/// é responsabilidade do consumer em Icarus.Infrastructure.
/// </summary>
public sealed record PersistirResultadoOnboardingCommand(
    Guid JobId,
    EpicoResultadoOnboarding Epico,
    CampanhaResultadoOnboarding Campanha,
    IReadOnlyList<MissaoResultadoOnboarding> Missoes);

/// <summary>Nunca inclui pontuação — ela é sempre calculada a partir da <see cref="Dificuldade"/> (MapeadorPontuacao).</summary>
public sealed record EpicoResultadoOnboarding(
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    DateOnly PrazoInicio,
    DateOnly? PrazoFim);

public sealed record CampanhaResultadoOnboarding(
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    DateOnly PrazoInicio,
    DateOnly? PrazoFim);

/// <param name="Horario">Horário sugerido no formato "HH:mm", ou nulo quando o worker não sugeriu um.</param>
public sealed record MissaoResultadoOnboarding(
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    DateOnly? DataLimite,
    string? Horario);
