using System.Text;
using System.Text.Json;
using Icarus.Application.Common;
using Icarus.Application.Interfaces;
using Icarus.Infrastructure.Messaging.Contratos;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Icarus.Infrastructure.Messaging;

public sealed class OnboardingJobPublisher(
    RabbitMqConnectionProvider connectionProvider,
    IOptions<RabbitMqOptions> options) : IOnboardingJobPublisher
{
    private readonly RabbitMqOptions options = options.Value;

    public async Task PublicarAsync(
        Guid jobId,
        Guid usuarioId,
        IReadOnlyList<RespostaOnboarding> respostas,
        CancellationToken cancellationToken)
    {
        var mensagem = new OnboardingJobMessage
        {
            JobId = jobId.ToString(),
            UserId = usuarioId.ToString(),
            Data = new OnboardingJobData
            {
                Answers = respostas
                    .Select(r => new OnboardingJobAnswer { Question = r.Pergunta, Answer = r.Respostas })
                    .ToList()
            }
        };

        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        var conexao = await connectionProvider.ObterConexaoAsync(cancellationToken);
        using var canal = await conexao.CreateChannelAsync(cancellationToken: cancellationToken);

        // Declaração defensiva e idempotente — mesmas propriedades que o worker já declara
        // (ADR-006 do icarus-ai); garante que a exchange existe mesmo se o .NET subir antes dele.
        await canal.ExchangeDeclareAsync(options.ExchangeEntrada, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);

        var propriedades = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await canal.BasicPublishAsync(
            options.ExchangeEntrada,
            options.RoutingKeyOnboarding,
            mandatory: false,
            propriedades,
            corpo,
            cancellationToken);
    }
}
