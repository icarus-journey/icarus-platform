using System.Text.Json;
using Icarus.Domain.Entities;
using Icarus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Icarus.Infrastructure.Messaging;

public sealed partial class PublicadorOutboxJornada(IServiceScopeFactory escopos, RabbitMqConnectionProvider conexoes,
    IOptions<RabbitMqOptions> opcoes, TimeProvider relogio, ILogger<PublicadorOutboxJornada> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { if (await PublicarAsync(stoppingToken)) continue; }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { LogPendente(); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    public async Task<bool> PublicarAsync(CancellationToken ct)
    {
        using var escopo = escopos.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<IcarusDbContext>();
        var agora = relogio.GetUtcNow();
        await using var transacao = await contexto.Database.BeginTransactionAsync(ct);
        var trabalho = await contexto.Set<OutboxJornada>().FromSqlInterpolated($"""
            SELECT * FROM outbox_jornada WHERE concluida_em IS NULL
              AND (proxima_tentativa_em IS NULL OR proxima_tentativa_em <= {agora})
              ORDER BY criada_em LIMIT 1 FOR UPDATE SKIP LOCKED
            """).SingleOrDefaultAsync(ct);
        if (trabalho is null) return false;
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(ct);
        prazo.CancelAfter(TimeSpan.FromSeconds(10));
        var conexao = await conexoes.ObterConexaoAsync(prazo.Token);
        await using var canal = await conexao.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), prazo.Token);
        await TopologiaJornada.DeclararAsync(canal, opcoes.Value, prazo.Token);
        await canal.BasicPublishAsync(opcoes.Value.ExchangeEntrada, TopologiaJornada.Rota, true,
            new BasicProperties { ContentType = "application/json", MessageId = trabalho.Id.ToString(), DeliveryMode = DeliveryModes.Persistent },
            JsonSerializer.SerializeToUtf8Bytes(new ComandoJornada("1.0.0", trabalho.Id), ComandoJornada.Json), prazo.Token);
        // Reenvio até a conclusão transacional: confirmação do broker não significa aplicação pela API.
        trabalho.ProximaTentativaEm = agora.AddMinutes(1);
        await contexto.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return true;
    }
    [LoggerMessage(Level = LogLevel.Warning, Message = "Publicação da jornada pendente; será retomada pela outbox")]
    private partial void LogPendente();
}
