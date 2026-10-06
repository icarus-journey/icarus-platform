using System.Reflection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;
using Serilog.Sinks.Grafana.Loki;

namespace Icarus.Api.Observability;

/// <summary>
/// Logs, traces e metricas (seção 11 do 01-arquitetura-da-solucao.md no icarus-context).
/// </summary>
public static class ObservabilityExtensions
{
    private const string NomeDoServico = "Icarus.Api";

    // Seção 11 do icarus-context pede "serviço, versão" em todo log — usamos a versão do
    // assembly (hoje o padrão do .NET quando o .csproj não fixa <Version> explicitamente).
    private static readonly string VersaoDoServico =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "desconhecida";

    /// <summary>
    /// Logs estruturados em JSON: no stdout (exigência da seção 11) e, em paralelo, no Loki.
    /// Enriquecidos com o TraceId/SpanId do OpenTelemetry — o Loki guarda o TraceId dentro do
    /// texto da linha (não como label, que seria alta cardinalidade), permitindo ao Grafana
    /// pular de um trace no Tempo direto para os logs daquela requisição, e vice-versa.
    /// </summary>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var lokiEndpoint = builder.Configuration["Observability:LokiEndpoint"] ?? "http://localhost:3100";

        builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .Enrich.WithProperty("Servico", NomeDoServico)
            .Enrich.WithProperty("Versao", VersaoDoServico)
            .WriteTo.Console(new JsonFormatter(renderMessage: true))
            .WriteTo.GrafanaLoki(
                lokiEndpoint,
                labels: [new LokiLabel { Key = "app", Value = NomeDoServico }],
                textFormatter: new JsonFormatter(renderMessage: true)));
    }

    /// <summary>
    /// Traces (via instrumentação automática do ASP.NET Core e do Npgsql) e metricas,
    /// exportados via OTLP para o Tempo e o Prometheus (icarus-infrastructure).
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var tempoEndpoint = configuration["Observability:TempoOtlpEndpoint"] ?? "http://localhost:4318/v1/traces";
        var prometheusEndpoint = configuration["Observability:PrometheusOtlpEndpoint"] ?? "http://localhost:9090/api/v1/otlp/v1/metrics";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(NomeDoServico, serviceVersion: VersaoDoServico))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                    // /health é chamado com muita frequência e não representa trabalho de
                    // negócio — excluído para não poluir o Tempo com milhares de traces vazios.
                    options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health"))
                .AddSource("Npgsql")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(tempoEndpoint);
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(prometheusEndpoint);
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                }));

        return services;
    }
}
