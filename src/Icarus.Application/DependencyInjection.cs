using Icarus.Application.UseCases.AtualizarCampanha;
using Icarus.Application.UseCases.AtualizarEpico;
using Icarus.Application.UseCases.AtualizarMissao;
using Icarus.Application.UseCases.CriarCampanha;
using Icarus.Application.UseCases.CriarEpico;
using Icarus.Application.UseCases.CriarMissao;
using Icarus.Application.UseCases.IniciarOnboarding;
using Icarus.Application.UseCases.ListarEpicos;
using Icarus.Application.UseCases.ListarMissoesAvulsas;
using Icarus.Application.UseCases.Login;
using Icarus.Application.UseCases.Logout;
using Icarus.Application.UseCases.MarcarOnboardingComoFalho;
using Icarus.Application.UseCases.ObterPerfil;
using Icarus.Application.UseCases.ObterStatusOnboarding;
using Icarus.Application.UseCases.PersistirResultadoOnboarding;
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
        services.AddScoped<IniciarOnboardingUseCase>();
        services.AddScoped<ObterStatusOnboardingUseCase>();
        services.AddScoped<PersistirResultadoOnboardingUseCase>();
        services.AddScoped<MarcarOnboardingComoFalhoUseCase>();
        services.AddScoped<CriarEpicoUseCase>();
        services.AddScoped<AtualizarEpicoUseCase>();
        services.AddScoped<ListarEpicosUseCase>();
        services.AddScoped<CriarCampanhaUseCase>();
        services.AddScoped<AtualizarCampanhaUseCase>();
        services.AddScoped<CriarMissaoUseCase>();
        services.AddScoped<AtualizarMissaoUseCase>();
        services.AddScoped<ListarMissoesAvulsasUseCase>();

        return services;
    }
}
