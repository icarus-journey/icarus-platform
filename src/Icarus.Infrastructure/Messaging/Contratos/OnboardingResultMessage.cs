using System.Text.Json.Serialization;

namespace Icarus.Infrastructure.Messaging.Contratos;

/// <summary>
/// Mensagem recebida em icarus.output.onboarding — espelha
/// icarus_worker.schemas.results.OnboardingResult no icarus-ai. Nunca traz pontuação, cor ou
/// priorização (decisão desta feature: backend calcula pontuação a partir da dificuldade;
/// cor/priorização são só do front). Campos desconhecidos (ex.: "estado"/"status" do worker,
/// que aqui são sempre o valor padrão da entidade) são ignorados na desserialização.
/// </summary>
public sealed class OnboardingResultMessage
{
    [JsonPropertyName("job_id")]
    public required string JobId { get; init; }

    [JsonPropertyName("user_id")]
    public required string UserId { get; init; }

    [JsonPropertyName("epic")]
    public required OnboardingResultEpic Epic { get; init; }

    [JsonPropertyName("campaign")]
    public required OnboardingResultCampaign Campaign { get; init; }

    [JsonPropertyName("missions")]
    public required IReadOnlyList<OnboardingResultMission> Missions { get; init; }
}

public sealed class OnboardingResultEpic
{
    [JsonPropertyName("titulo")]
    public required string Titulo { get; init; }

    [JsonPropertyName("area_da_vida")]
    public required string AreaDaVida { get; init; }

    [JsonPropertyName("dificuldade")]
    public required string Dificuldade { get; init; }

    [JsonPropertyName("descricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("prazo_inicio")]
    public required DateOnly PrazoInicio { get; init; }

    [JsonPropertyName("prazo_fim")]
    public DateOnly? PrazoFim { get; init; }
}

public sealed class OnboardingResultCampaign
{
    [JsonPropertyName("titulo")]
    public required string Titulo { get; init; }

    [JsonPropertyName("area_da_vida")]
    public required string AreaDaVida { get; init; }

    [JsonPropertyName("dificuldade")]
    public required string Dificuldade { get; init; }

    [JsonPropertyName("descricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("prazo_inicio")]
    public required DateOnly PrazoInicio { get; init; }

    [JsonPropertyName("prazo_fim")]
    public DateOnly? PrazoFim { get; init; }
}

public sealed class OnboardingResultMission
{
    [JsonPropertyName("titulo")]
    public required string Titulo { get; init; }

    [JsonPropertyName("descricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("beneficio")]
    public string? Beneficio { get; init; }

    [JsonPropertyName("dificuldade")]
    public required string Dificuldade { get; init; }

    [JsonPropertyName("horario")]
    public string? Horario { get; init; }

    [JsonPropertyName("data_limite")]
    public DateOnly? DataLimite { get; init; }
}
