namespace Icarus.Application.UseCases.MarcarOnboardingComoFalho;

/// <summary>
/// Entrada do caso de uso que marca um trabalho de onboarding como falho — chamado pelo
/// consumer RabbitMQ quando uma mensagem (job ou resultado) esgota as tentativas e vai pra
/// DLQ, pra que o status pare de ficar "Pendente" indefinidamente.
/// </summary>
public sealed record MarcarOnboardingComoFalhoCommand(Guid JobId, string ErroMensagem);
