using Icarus.Application.UseCases.UtilizarRecompensa;
using Icarus.Application.UseCases.ResgatarRecompensa;
using Icarus.Application.UseCases.ExcluirRecompensa;
using Icarus.Application.UseCases.DesfazerResgate;
using Icarus.Application.UseCases.CriarRecompensa;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Application.UseCases.AtualizarRecompensa;
using Icarus.Application.UseCases.AtualizarCampanha;
using Icarus.Application.UseCases.AtualizarEpico;
using Icarus.Application.UseCases.AtualizarMissao;
using Icarus.Application.UseCases.ConcluirCampanha;
using Icarus.Application.UseCases.ConcluirEpico;
using Icarus.Application.UseCases.ConcluirMissao;
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
using Icarus.Application.UseCases.AtualizarPerfil;
using Icarus.Application.UseCases.AtualizarFuso;
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
        services.AddScoped<ConsultarLojaUseCase>();
        services.AddScoped<CriarRecompensaUseCase>();
        services.AddScoped<AtualizarRecompensaUseCase>();
        services.AddScoped<ExcluirRecompensaUseCase>();
        services.AddScoped<ResgatarRecompensaUseCase>();
        services.AddScoped<DesfazerResgateUseCase>();
        services.AddScoped<UtilizarRecompensaUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<ObterPerfilUseCase>();
        services.AddScoped<AtualizarPerfilUseCase>();
        services.AddScoped<AtualizarFusoUseCase>();
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
        services.AddScoped<ConcluirMissaoUseCase>();
        services.AddScoped<ConcluirCampanhaUseCase>();
        services.AddScoped<ConcluirEpicoUseCase>();

        return services;
    }
}
