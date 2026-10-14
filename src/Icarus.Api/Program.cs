using Icarus.Api;
using Icarus.Api.Observability;
using Icarus.Application;
using Icarus.Infrastructure;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureSerilog();

// Adiciona os serviços ao container.
builder.Services.AddApi(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
app.UseSerilogRequestLogging(options =>
{
    // /health tende a ser chamado com muita frequência (orquestrador, Prometheus) — Debug em
    // vez de Information evita que isso afogue os logs de requisições reais.
    options.GetLevel = (httpContext, _, _) =>
        httpContext.Request.Path.StartsWithSegments("/health")
            ? LogEventLevel.Debug
            : LogEventLevel.Information;
});

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Swagger UI (Swashbuckle) lendo o documento OpenAPI que o MapOpenApi acima já gera —
    // sem AddSwaggerGen: não precisamos de um segundo gerador de documento, só da UI.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Icarus API");

        // Mantém o token colado no botão "Authorize" entre uma chamada e outra
        // (sem isso, teria que colar o token de novo a cada F5 na página).
        options.ConfigObject.PersistAuthorization = true;
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
