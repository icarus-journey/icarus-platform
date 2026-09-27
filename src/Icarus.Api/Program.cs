using Icarus.Api;
using Icarus.Application;
using Icarus.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços ao container.
builder.Services.AddApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
