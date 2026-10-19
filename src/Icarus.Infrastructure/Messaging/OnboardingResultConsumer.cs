using System.Globalization;
using System.Text;
using System.Text.Json;
using Icarus.Application.UseCases.MarcarOnboardingComoFalho;
using Icarus.Application.UseCases.PersistirResultadoOnboarding;
using Icarus.Infrastructure.Messaging.Contratos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Icarus.Infrastructure.Messaging;

/// <summary>
/// Consome icarus.output.onboarding e persiste o resultado. Erro de desserialização
/// (mensagem envenenada) ou tentativas esgotadas vão para uma DLQ própria — o worker não
/// configura DLQ nas filas de saída, então essa malha de segurança é só deste lado.
/// </summary>
public sealed partial class OnboardingResultConsumer(
    RabbitMqConnectionProvider connectionProvider,
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<OnboardingResultConsumer> logger) : BackgroundService
{
    private const string CabecalhoTentativas = "x-tentativas";
    private readonly RabbitMqOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var conexao = await connectionProvider.ObterConexaoAsync(stoppingToken);
        var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);

        await canal.ExchangeDeclareAsync(options.ExchangeSaida, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await canal.QueueDeclareAsync(options.FilaResultadoOnboarding, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await canal.QueueBindAsync(options.FilaResultadoOnboarding, options.ExchangeSaida, options.RoutingKeyOnboarding, cancellationToken: stoppingToken);
        await canal.QueueDeclareAsync(options.FilaResultadoOnboardingDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        await canal.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumidor = new AsyncEventingBasicConsumer(canal);
        consumidor.ReceivedAsync += async (_, ea) => await ProcessarMensagemAsync(canal, ea, stoppingToken);

        await canal.BasicConsumeAsync(options.FilaResultadoOnboarding, autoAck: false, consumidor, cancellationToken: stoppingToken);

        // Mantém o BackgroundService vivo enquanto a aplicação não for encerrada — o consumo
        // em si acontece nos callbacks do AsyncEventingBasicConsumer, não neste loop.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task ProcessarMensagemAsync(IChannel canal, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        OnboardingResultMessage? mensagem;
        try
        {
            mensagem = JsonSerializer.Deserialize<OnboardingResultMessage>(ea.Body.Span);
            if (mensagem is null)
            {
                throw new JsonException("Corpo da mensagem é nulo.");
            }
        }
        catch (Exception ex)
        {
            LogMensagemInvalida(ex);
            await EnviarParaDlqAsync(canal, ea, stoppingToken);
            await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        try
        {
            using var escopo = scopeFactory.CreateScope();
            var persistir = escopo.ServiceProvider.GetRequiredService<PersistirResultadoOnboardingUseCase>();

            var comando = new PersistirResultadoOnboardingCommand(
                Guid.Parse(mensagem.JobId),
                new EpicoResultadoOnboarding(
                    mensagem.Epic.Titulo,
                    mensagem.Epic.Descricao,
                    OnboardingWireMapeamento.ParaAreaDaVida(mensagem.Epic.AreaDaVida),
                    OnboardingWireMapeamento.ParaDificuldade(mensagem.Epic.Dificuldade),
                    mensagem.Epic.PrazoInicio,
                    mensagem.Epic.PrazoFim),
                new CampanhaResultadoOnboarding(
                    mensagem.Campaign.Titulo,
                    mensagem.Campaign.Descricao,
                    OnboardingWireMapeamento.ParaAreaDaVida(mensagem.Campaign.AreaDaVida),
                    OnboardingWireMapeamento.ParaDificuldade(mensagem.Campaign.Dificuldade),
                    mensagem.Campaign.PrazoInicio,
                    mensagem.Campaign.PrazoFim),
                mensagem.Missions.Select(m => new MissaoResultadoOnboarding(
                    m.Titulo,
                    m.Descricao,
                    m.Beneficio,
                    OnboardingWireMapeamento.ParaDificuldade(m.Dificuldade),
                    m.DataLimite,
                    m.Horario)).ToList());

            await persistir.ExecuteAsync(comando, stoppingToken);
            await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            await TratarFalhaAsync(canal, ea, mensagem.JobId, ex, stoppingToken);
        }
    }

    private async Task TratarFalhaAsync(IChannel canal, BasicDeliverEventArgs ea, string jobId, Exception erro, CancellationToken stoppingToken)
    {
        var tentativas = ObterTentativas(ea);

        if (tentativas < options.MaxTentativas)
        {
            LogFalhaTransiente(jobId, tentativas + 1, erro);

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativas)), stoppingToken);

            var propriedades = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Headers = new Dictionary<string, object?> { [CabecalhoTentativas] = tentativas + 1 }
            };

            await canal.BasicPublishAsync(options.ExchangeSaida, options.RoutingKeyOnboarding, mandatory: false, propriedades, ea.Body, stoppingToken);
            await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        LogTentativasEsgotadas(jobId, erro);

        using var escopo = scopeFactory.CreateScope();
        var marcarFalho = escopo.ServiceProvider.GetRequiredService<MarcarOnboardingComoFalhoUseCase>();
        await marcarFalho.ExecuteAsync(new MarcarOnboardingComoFalhoCommand(Guid.Parse(jobId), erro.Message), stoppingToken);

        await EnviarParaDlqAsync(canal, ea, stoppingToken);
        await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
    }

    private static int ObterTentativas(BasicDeliverEventArgs ea)
    {
        if (ea.BasicProperties.Headers is { } headers
            && headers.TryGetValue(CabecalhoTentativas, out var valor)
            && valor is not null)
        {
            return valor switch
            {
                int i => i,
                long l => (int)l,
                byte[] bytes => int.Parse(Encoding.UTF8.GetString(bytes), CultureInfo.InvariantCulture),
                _ => 0
            };
        }

        return 0;
    }

    private Task EnviarParaDlqAsync(IChannel canal, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        => canal.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: options.FilaResultadoOnboardingDlq,
            mandatory: false,
            new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent },
            ea.Body,
            cancellationToken).AsTask();

    [LoggerMessage(Level = LogLevel.Error, Message = "Mensagem de resultado de onboarding inválida — enviada para a DLQ")]
    private partial void LogMensagemInvalida(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha transiente ao persistir onboarding {JobId}, tentativa {Tentativa}")]
    private partial void LogFalhaTransiente(string jobId, int tentativa, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Onboarding {JobId} esgotou as tentativas — enviado para a DLQ")]
    private partial void LogTentativasEsgotadas(string jobId, Exception ex);
}
