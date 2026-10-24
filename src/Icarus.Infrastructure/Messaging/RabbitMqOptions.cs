namespace Icarus.Infrastructure.Messaging;

/// <summary>
/// Configuração do RabbitMQ. <see cref="Usuario"/>/<see cref="Senha"/> são segredo (User
/// Secrets em dev, nunca em appsettings.json) — campos discretos, não uma connection string
/// única, porque a senha padrão do icarus-infrastructure contém um '@' literal que quebraria
/// o parsing de URI (dois '@' na mesma string). Os nomes de exchange/fila não são segredos e
/// ficam em appsettings.json — mesmos defaults do worker icarus-ai (docs/03-arquitetura.md).
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SecaoConfiguracao = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Porta { get; set; } = 5672;
    public string Usuario { get; set; } = "guest";
    public string Senha { get; set; } = "guest";
    public string ExchangeEntrada { get; set; } = "icarus.input";
    public string ExchangeSaida { get; set; } = "icarus.output";
    public string RoutingKeyOnboarding { get; set; } = "onboarding";
    public string FilaResultadoOnboarding { get; set; } = "icarus.output.onboarding";
    public string FilaResultadoOnboardingDlq { get; set; } = "icarus.output.onboarding.dlq";
    public int MaxTentativas { get; set; } = 3;
}
