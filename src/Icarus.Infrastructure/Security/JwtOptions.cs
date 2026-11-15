namespace Icarus.Infrastructure.Security;

/// <summary>
/// Configuração do JWT (seção "Jwt" em appsettings/User Secrets). A chave
/// secreta nunca fica em appsettings.json — segue a mesma convenção da
/// connection string, guardada via `dotnet user-secrets` no desenvolvimento.
/// </summary>
public sealed class JwtOptions
{
    public const string SecaoConfiguracao = "Jwt";

    public string ChaveSecreta { get; set; } = string.Empty;
    public string Emissor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;
}
