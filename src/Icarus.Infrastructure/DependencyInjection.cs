using Icarus.Infrastructure.Persistence;
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

        services.AddDbContext<IcarusDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
