using Icarus.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Icarus.Infrastructure.Jobs;

/// <summary>Inicia campanhas sem depender de o aplicativo estar aberto.</summary>
public sealed partial class InicioCampanhasService(IServiceScopeFactory escopos, ILogger<InicioCampanhasService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var escopo = escopos.CreateScope();
                await escopo.ServiceProvider.GetRequiredService<IIniciadorCampanhas>().ExecuteAsync(null, stoppingToken);
            }
            catch (Exception erro) when (erro is not OperationCanceledException)
            {
                LogFalha(erro);
            }
        } while (await temporizador.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao iniciar campanhas planejadas; nova tentativa no próximo minuto")]
    private partial void LogFalha(Exception erro);
}
