using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Icarus.Infrastructure.Messaging;

public static class TopologiaRelatorioSemanal
{
    public const string Rota = "relatorio.semanal.v1";
    public const string Entrada = "icarus.relatorio.semanal.v1";
    public const string Saida = "icarus.output.relatorio.semanal.v1";
    public const string DlqEntrada = Entrada + ".dlq";
    public const string DlqSaida = Saida + ".dlq";

    public static async Task DeclararAsync(IChannel canal, RabbitMqOptions opcoes, CancellationToken ct)
    {
        await canal.ExchangeDeclareAsync(opcoes.ExchangeEntrada, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await canal.ExchangeDeclareAsync(opcoes.ExchangeSaida, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await canal.QueueDeclareAsync(DlqEntrada, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await canal.QueueDeclareAsync(DlqSaida, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await canal.QueueDeclareAsync(Entrada, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = DlqEntrada }, cancellationToken: ct);
        await canal.QueueDeclareAsync(Saida, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = DlqSaida }, cancellationToken: ct);
        await canal.QueueBindAsync(Entrada, opcoes.ExchangeEntrada, Rota, cancellationToken: ct);
        await canal.QueueBindAsync(Saida, opcoes.ExchangeSaida, Rota, cancellationToken: ct);
    }
}
