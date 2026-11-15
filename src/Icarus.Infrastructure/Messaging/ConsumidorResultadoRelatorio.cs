using System.Text.Json;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.UseCases.PersistirRelatorioSemanal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Icarus.Infrastructure.Messaging;

public sealed partial class ConsumidorResultadoRelatorio(IServiceScopeFactory escopos,
    RabbitMqConnectionProvider conexoes, IOptions<RabbitMqOptions> configuracao,
    ILogger<ConsumidorResultadoRelatorio> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var conexao = await conexoes.ObterConexaoAsync(stoppingToken);
                await using var canal = await conexao.CreateChannelAsync(new CreateChannelOptions(
                    publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), stoppingToken);
                await TopologiaRelatorioSemanal.DeclararAsync(canal, configuracao.Value, stoppingToken);
                await canal.BasicQosAsync(0, 1, false, stoppingToken);
                var consumidor = new AsyncEventingBasicConsumer(canal);
                consumidor.ReceivedAsync += async (_, entrada) =>
                {
                    ResultadoRelatorioSemanal? mensagem;
                    try
                    {
                        if (entrada.Body.Length > 65536) throw new JsonException();
                        mensagem = JsonSerializer.Deserialize<ResultadoRelatorioSemanal>(entrada.Body.Span, ContratoRelatorioSemanal.Json);
                        if (mensagem is null || mensagem.IdMensagem == Guid.Empty || mensagem.CorrelacaoId == Guid.Empty
                            || mensagem.CausalidadeId == Guid.Empty) throw new JsonException();
                    }
                    catch (JsonException)
                    {
                        LogRejeitada();
                        await canal.BasicRejectAsync(entrada.DeliveryTag, false, stoppingToken); return;
                    }
                    try
                    {
                        using var escopo = escopos.CreateScope();
                        await escopo.ServiceProvider.GetRequiredService<PersistirRelatorioSemanalUseCase>().ExecuteAsync(mensagem, stoppingToken);
                        await canal.BasicAckAsync(entrada.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        LogPersistenciaPendente();
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        var tentativas = entrada.BasicProperties.Headers?.TryGetValue("x-icarus-tentativas", out var valor) == true
                            && valor is int numero ? numero : 0;
                        if (tentativas >= 2)
                        {
                            await canal.BasicRejectAsync(entrada.DeliveryTag, false, stoppingToken);
                            return;
                        }
                        var propriedades = new BasicProperties(entrada.BasicProperties)
                        {
                            Headers = entrada.BasicProperties.Headers is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(entrada.BasicProperties.Headers),
                            DeliveryMode = DeliveryModes.Persistent
                        };
                        propriedades.Headers["x-icarus-tentativas"] = tentativas + 1;
                        // Confirma a cópia durável antes de reconhecer a entrega original.
                        // Se a confirmação se perder, a inbox torna uma cópia duplicada inofensiva.
                        await canal.BasicPublishAsync("", TopologiaRelatorioSemanal.Saida, true,
                            propriedades, entrada.Body, stoppingToken);
                        await canal.BasicAckAsync(entrada.DeliveryTag, false, stoppingToken);
                    }
                };
                await canal.BasicConsumeAsync(TopologiaRelatorioSemanal.Saida, false, consumidor, stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { LogPersistenciaPendente(); await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
        }
    }
    [LoggerMessage(Level = LogLevel.Warning, Message = "Resultado de relatório rejeitado por contrato; encaminhado à fila de mensagens não processáveis")]
    private partial void LogRejeitada();
    [LoggerMessage(Level = LogLevel.Warning, Message = "Recepção de relatório pendente; transporte será retomado sem aplicar resultado parcial")]
    private partial void LogPersistenciaPendente();
}
