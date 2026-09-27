using Icarus.Application.UseCases.RegisterUser;
using Microsoft.Extensions.DependencyInjection;

namespace Icarus.Application;

/// <summary>
/// Métodos de extensão responsáveis por registrar os serviços da camada de aplicação.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserUseCase>();

        return services;
    }
}
