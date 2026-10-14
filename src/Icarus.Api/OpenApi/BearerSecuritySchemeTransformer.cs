using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Icarus.Api.OpenApi;

/// <summary>
/// Adiciona o esquema de segurança Bearer (JWT) ao documento OpenAPI e marca os
/// endpoints com <see cref="AuthorizeAttribute"/> como exigindo-o — é o que faz o
/// botão "Authorize" aparecer no Swagger UI. Como não usamos AddSwaggerGen
/// (só a UI, lendo o documento do Microsoft.AspNetCore.OpenApi), não existe
/// AddSecurityDefinition do Swashbuckle aqui; a definição precisa entrar no
/// próprio documento via um transformer.
/// </summary>
public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string NomeDoEsquema = "Bearer";

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var esquemasRegistrados = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!esquemasRegistrados.Any(esquema => esquema.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[NomeDoEsquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT"
        };
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // Só marca como protegido quem tem [Authorize] de verdade — cadastro e login
        // continuam sem o cadeado, já que são endpoints deliberadamente públicos.
        var exigeAutorizacao = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (!exigeAutorizacao)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(NomeDoEsquema, context.Document)] = []
        });

        return Task.CompletedTask;
    }
}
