using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Icarus.Api.Contracts.Auth;
using Icarus.Api.Contracts.Campanhas;
using Icarus.Api.Contracts.Epicos;
using Icarus.Api.Contracts.Missoes;
using Icarus.Api.Contracts.Usuarios;
using Icarus.Api.ExceptionHandling;
using Icarus.Api.Filters;
using Icarus.Api.OpenApi;
using Icarus.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Icarus.Api;

/// <summary>
/// Métodos de extensão responsáveis por registrar os serviços da camada de API.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Mensagens de validação em português (o FluentValidation já traz as traduções pt-BR).
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");

        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();

            // Campo ausente vira null e cai no FluentValidation (mensagem em pt-BR), em vez do
            // "required" implícito do MVC, que responderia antes em inglês.
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        }).AddJsonOptions(options =>
        {
            // Primeiro enum exposto via HTTP (onboarding) — camelCase consistente com o resto
            // do JSON da API (ex.: "concluido", "saude", "facil"), em vez do valor numérico padrão.
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        // Saiba mais sobre como configurar o OpenAPI em https://aka.ms/aspnet/openapi
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Validadores são stateless, então singleton. Um por Request; o ValidationFilter os encontra sozinho.
        services.AddSingleton<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddSingleton<IValidator<RefreshRequest>, RefreshRequestValidator>();
        services.AddSingleton<IValidator<IniciarOnboardingRequest>, IniciarOnboardingRequestValidator>();
        services.AddSingleton<IValidator<CriarEpicoRequest>, CriarEpicoRequestValidator>();
        services.AddSingleton<IValidator<AtualizarEpicoRequest>, AtualizarEpicoRequestValidator>();
        services.AddSingleton<IValidator<CriarCampanhaRequest>, CriarCampanhaRequestValidator>();
        services.AddSingleton<IValidator<AtualizarCampanhaRequest>, AtualizarCampanhaRequestValidator>();
        services.AddSingleton<IValidator<CriarMissaoRequest>, CriarMissaoRequestValidator>();
        services.AddSingleton<IValidator<AtualizarMissaoRequest>, AtualizarMissaoRequestValidator>();

        // "Saúde da API" (seção 11 do icarus-context): /health responde 200 só se também
        // conseguir falar com o Postgres — não é só "o processo está de pé", é "a API funciona".
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada.");

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        // Lado da validação do JWT (RF-02.4) — o lado da emissão já existe em
        // Icarus.Infrastructure/Security/JwtTokenGenerator.cs. Mesma chave/emissor/audiência
        // dos dois lados, senão nenhum token emitido validaria aqui.
        var jwtOptions = configuration.GetSection(JwtOptions.SecaoConfiguracao).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Configuração 'Jwt' não encontrada.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Emissor,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audiencia,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.ChaveSecreta))
            });

        services.AddAuthorization();

        return services;
    }
}
