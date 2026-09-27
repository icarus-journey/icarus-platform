using System.Globalization;
using FluentValidation;
using Icarus.Api.Contracts.Auth;
using Icarus.Api.ExceptionHandling;
using Icarus.Api.Filters;

namespace Icarus.Api;

/// <summary>
/// Métodos de extensão responsáveis por registrar os serviços da camada de API.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        // Mensagens de validação em português (o FluentValidation já traz as traduções pt-BR).
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");

        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();

            // Campo ausente vira null e cai no FluentValidation (mensagem em pt-BR), em vez do
            // "required" implícito do MVC, que responderia antes em inglês.
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });

        // Saiba mais sobre como configurar o OpenAPI em https://aka.ms/aspnet/openapi
        services.AddOpenApi();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Validadores são stateless, então singleton. Um por Request; o ValidationFilter os encontra sozinho.
        services.AddSingleton<IValidator<RegisterRequest>, RegisterRequestValidator>();

        return services;
    }
}
