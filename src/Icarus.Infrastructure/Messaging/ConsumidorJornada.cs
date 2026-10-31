using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Icarus.Infrastructure.Messaging;

public sealed partial class ConsumidorJornada(IServiceScopeFactory escopos, RabbitMqConnectionProvider conexoes,
    IOptions<RabbitMqOptions> opcoes, ILogger<ConsumidorJornada> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var conexao = await conexoes.ObterConexaoAsync(stoppingToken);
                await using var canal = await conexao.CreateChannelAsync(cancellationToken: stoppingToken);
                await TopologiaJornada.DeclararAsync(canal, opcoes.Value, stoppingToken);
                await canal.BasicQosAsync(0, 1, false, stoppingToken);
                var consumidor = new AsyncEventingBasicConsumer(canal);
                consumidor.ReceivedAsync += async (_, entrada) =>
                {
                    ComandoJornada comando;
                    try { comando = ComandoJornada.Ler(entrada.Body.Span); }
                    catch (JsonException)
                    {
                        LogRejeitado();
                        await canal.BasicRejectAsync(entrada.DeliveryTag, false, stoppingToken);
                        return;
                    }
                    try
                    {
                        using var escopo = escopos.CreateScope();
                        await escopo.ServiceProvider.GetRequiredService<ProcessadorJornada>().ExecutarAsync(comando, stoppingToken);
                        await canal.BasicAckAsync(entrada.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        LogPendente();
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        await canal.BasicNackAsync(entrada.DeliveryTag, false, true, stoppingToken);
                    }
                };
                await canal.BasicConsumeAsync(TopologiaJornada.Saida, false, consumidor, stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception)
            {
                LogReconexao();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
    [LoggerMessage(Level = LogLevel.Warning, Message = "Comando de jornada rejeitado por contrato")]
    private partial void LogRejeitado();
    [LoggerMessage(Level = LogLevel.Warning, Message = "Fechamento da jornada pendente; entrega será repetida")]
    private partial void LogPendente();
    [LoggerMessage(Level = LogLevel.Warning, Message = "Recepção da jornada indisponível; reconexão pendente")]
    private partial void LogReconexao();
}
