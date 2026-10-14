using Icarus.Application.UseCases.Login;
using Icarus.Application.UseCases.Logout;
using Icarus.Application.UseCases.ObterPerfil;
using Icarus.Application.UseCases.RefreshToken;
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
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<ObterPerfilUseCase>();

        return services;
    }
}
