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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
