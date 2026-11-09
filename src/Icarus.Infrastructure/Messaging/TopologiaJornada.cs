using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;

namespace Icarus.Infrastructure.Messaging;

public sealed record ComandoJornada(string VersaoContrato, Guid? TrabalhoId)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };
    public static ComandoJornada Ler(ReadOnlySpan<byte> carga)
    {
        if (carga.Length > 1024) throw new JsonException();
        var comando = JsonSerializer.Deserialize<ComandoJornada>(carga, Json);
        if (comando is null || comando.VersaoContrato != "1.0.0" || comando.TrabalhoId == Guid.Empty)
            throw new JsonException();
        return comando;
    }
}

public static class TopologiaJornada
{
    public const string Rota = "jornada.fechamento.v1";
    public const string Entrada = "icarus.jornada.fechamento.v1";
    public const string Saida = "icarus.output.jornada.fechamento.v1";

    public static async Task DeclararAsync(IChannel canal, RabbitMqOptions opcoes, CancellationToken ct)
    {
        await canal.ExchangeDeclareAsync(opcoes.ExchangeEntrada, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await canal.ExchangeDeclareAsync(opcoes.ExchangeSaida, ExchangeType.Direct, durable: true, cancellationToken: ct);
        foreach (var (nome, exchange) in new[] { (Entrada, opcoes.ExchangeEntrada), (Saida, opcoes.ExchangeSaida) })
        {
            await canal.QueueDeclareAsync(nome + ".dlq", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await canal.QueueDeclareAsync(nome, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = nome + ".dlq" }, cancellationToken: ct);
            await canal.QueueBindAsync(nome, exchange, Rota, cancellationToken: ct);
        }
    }
}
