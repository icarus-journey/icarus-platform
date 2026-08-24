using Icarus.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços ao container.

builder.Services.AddControllers();
// Saiba mais sobre como configurar o OpenAPI em https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
