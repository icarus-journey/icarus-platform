using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Application.Interfaces;
using Icarus.Infrastructure.Messaging;
using Icarus.Infrastructure.Persistence;
using Icarus.Infrastructure.Persistence.Repositories;
using Icarus.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icarus.Infrastructure;

/// <summary>
/// Métodos de extensão responsáveis por registrar os serviços da camada de infraestrutura.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra o contexto de banco de dados e demais serviços da infraestrutura no container de injeção de dependência.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada.");

        services.AddDbContext<IcarusDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SecaoConfiguracao));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // RF-02.3: contador em memória, por combinação de e-mail + origem. Singleton
        // porque o estado (as tentativas) precisa persistir entre requisições diferentes.
        services.AddMemoryCache();
        services.AddSingleton<ILoginAttemptLimiter, LoginAttemptLimiter>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IReferenciasPublicasRepository, ReferenciasPublicasRepository>();
        services.AddScoped<IIndicadoresPessoaisRepository, IndicadoresPessoaisRepository>();
        services.AddScoped<IRelatorioSemanalRepository, RelatorioSemanalRepository>();
        services.AddScoped<ILojaRepository, LojaRepository>();
        var loja = configuration.GetSection("Loja").Get<LojaOptions>() ?? new();
        if (loja.CustoProtecao is < 1 or > 1_000_000)
            throw new InvalidOperationException("Loja:CustoProtecao deve estar entre 1 e 1.000.000.");
        services.AddSingleton(loja);
        services.AddScoped<ITokenRenovacaoRepository, TokenRenovacaoRepository>();
        services.AddScoped<IEpicoRepository, EpicoRepository>();
        services.AddScoped<ICampanhaRepository, CampanhaRepository>();
        services.AddScoped<IMissaoRepository, MissaoRepository>();
        services.AddScoped<IOcorrenciaRepository, OcorrenciaRepository>();
        services.AddScoped<IMovimentacaoPontosRepository, MovimentacaoPontosRepository>();
        services.AddScoped<ITrabalhoOnboardingRepository, TrabalhoOnboardingRepository>();

        // Mesma instância de IcarusDbContext do escopo (não uma nova) — repositório e
        // unit of work precisam compartilhar o ChangeTracker para salvar na mesma transação.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<IcarusDbContext>());

        // RabbitMQ (onboarding por IA, icarus-ai): conexão única compartilhada, publisher
        // escopado (chamado de dentro de um caso de uso) e o consumer do resultado como
        // hosted service de longa duração.
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SecaoConfiguracao));
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddScoped<IOnboardingJobPublisher, OnboardingJobPublisher>();
        services.AddHostedService<OnboardingResultConsumer>();
        services.AddHostedService<PublicadorOutboxRelatorio>();
        services.AddHostedService<ConsumidorResultadoRelatorio>();

        return services;
    }
}
