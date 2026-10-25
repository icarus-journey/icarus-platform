using Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Icarus.Infrastructure.Jobs;

/// <summary>
/// Mantém a janela de 30 dias de ocorrências das missões recorrentes ativas (RF-04.5). O
/// cálculo em si fica em <see cref="EstenderJanelaDeOcorrenciasUseCase"/> — este serviço só
/// cuida do agendamento, abrindo um <see cref="IServiceScope"/> por execução (mesmo padrão de
/// <see cref="Icarus.Infrastructure.Messaging.OnboardingResultConsumer"/>).
///
/// Simplificação em relação ao design original (arquitetura seção 9.4), que previa o worker
/// Python coordenando o timing via RabbitMQ: aqui o próprio .NET agenda com um
/// <see cref="PeriodicTimer"/> local, sem envolver o icarus-ai — mais simples e suficiente
/// para o estágio atual do produto.
/// </summary>
public sealed partial class RecorrenciaMaintenanceService(
    IServiceScopeFactory scopeFactory,
    ILogger<RecorrenciaMaintenanceService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);

        // Roda uma vez já na subida (não espera 24h) — cobre o caso de a API ter ficado
        // parada por mais tempo que o intervalo e a janela precisar de reforço imediato.
        await ExecutarAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecutarAsync(stoppingToken);
        }
    }

    private async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var escopo = scopeFactory.CreateScope();
            await escopo.ServiceProvider.GetRequiredService<IIniciadorCampanhas>().ExecuteAsync(null, cancellationToken);
            var useCase = escopo.ServiceProvider.GetRequiredService<EstenderJanelaDeOcorrenciasUseCase>();
            await useCase.ExecuteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFalha(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao manter a janela de ocorrências de missões recorrentes")]
    private partial void LogFalha(Exception ex);
}
