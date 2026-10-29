using System.Text;
using Icarus.Domain.Entities;
using Icarus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Icarus.Infrastructure.Messaging;

public sealed partial class PublicadorOutboxRelatorio(IServiceScopeFactory escopos,
    RabbitMqConnectionProvider conexoes, IOptions<RabbitMqOptions> configuracao,
    TimeProvider relogio, ILogger<PublicadorOutboxRelatorio> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await PublicarPendenteAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { LogPublicacaoPendente(); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task<bool> PublicarPendenteAsync(CancellationToken ct)
    {
        using var escopo = escopos.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<IcarusDbContext>();
        var agora = relogio.GetUtcNow();
        await using var transacao = await contexto.Database.BeginTransactionAsync(ct);
        var mensagem = await contexto.Set<OutboxRelatorio>().FromSqlInterpolated($"""
            SELECT * FROM outbox_relatorio WHERE publicada_em IS NULL AND carga IS NOT NULL
              AND proxima_tentativa_em <= {agora} ORDER BY criado_em LIMIT 1 FOR UPDATE SKIP LOCKED
            """).SingleOrDefaultAsync(ct);
        if (mensagem is null) return false;
        var trabalho = await contexto.Set<TrabalhoRelatorioSemanal>().SingleAsync(t => t.Id == mensagem.TrabalhoId, ct);
        if (trabalho.TentativaId != mensagem.Id || trabalho.Estado != "processando" || trabalho.ExpiraEm <= agora)
        {
            mensagem.Carga = null;
            if (trabalho.TentativaId == mensagem.Id && trabalho.Estado == "processando")
            { trabalho.Estado = "falha"; trabalho.FalhaCodigo = "prazo_expirado"; }
        }
        else
        {
            try
            {
                using var prazo = CancellationTokenSource.CreateLinkedTokenSource(ct); prazo.CancelAfter(TimeSpan.FromSeconds(10));
                var conexao = await conexoes.ObterConexaoAsync(prazo.Token);
                await using var canal = await conexao.CreateChannelAsync(new CreateChannelOptions(
                    publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), prazo.Token);
                await TopologiaRelatorioSemanal.DeclararAsync(canal, configuracao.Value, prazo.Token);
                await canal.BasicPublishAsync(configuracao.Value.ExchangeEntrada, TopologiaRelatorioSemanal.Rota, true,
                    new BasicProperties { ContentType = "application/json", MessageId = mensagem.Id.ToString(), DeliveryMode = DeliveryModes.Persistent },
                    Encoding.UTF8.GetBytes(mensagem.Carga!), prazo.Token);
                mensagem.PublicadaEm = agora; mensagem.Carga = null;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                mensagem.Tentativas++;
                mensagem.ProximaTentativaEm = agora.AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(6, mensagem.Tentativas))));
                LogPublicacaoPendente();
            }
        }
        await contexto.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return true;
    }
    [LoggerMessage(Level = LogLevel.Warning, Message = "Publicação de relatório pendente; nova tentativa seguirá a outbox")]
    private partial void LogPublicacaoPendente();
}
