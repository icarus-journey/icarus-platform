using System.Text.Json.Serialization;

namespace Icarus.Infrastructure.Messaging.Contratos;

/// <summary>
/// Mensagem publicada em icarus.input (routing key "onboarding") — espelha
/// icarus_worker.schemas.jobs.OnboardingJob no icarus-ai. Nomes em snake_case: contrato entre
/// serviços, não API HTTP — não segue a convenção camelCase do resto deste projeto.
/// </summary>
public sealed class OnboardingJobMessage
{
    [JsonPropertyName("job_id")]
    public required string JobId { get; init; }

    [JsonPropertyName("user_id")]
    public required string UserId { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "onboarding";

    [JsonPropertyName("data")]
    public required OnboardingJobData Data { get; init; }
}

public sealed class OnboardingJobData
{
    [JsonPropertyName("answers")]
    public required IReadOnlyList<OnboardingJobAnswer> Answers { get; init; }
}

public sealed class OnboardingJobAnswer
{
    [JsonPropertyName("question")]
    public required string Question { get; init; }

    /// <summary>Sempre lista, mesmo com 1 item — Pydantic (str | list[str]) aceita list[str] com 1 item.</summary>
    [JsonPropertyName("answer")]
    public required IReadOnlyList<string> Answer { get; init; }
}
