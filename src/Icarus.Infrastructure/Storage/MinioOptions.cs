namespace Icarus.Infrastructure.Storage;

/// <summary>
/// Configuração do MinIO (seção "Minio" em appsettings/User Secrets).
/// <see cref="AccessKey"/>/<see cref="SecretKey"/> são segredo — User Secrets em
/// desenvolvimento, nunca em appsettings.json, mesma convenção da connection string.
/// </summary>
public sealed class MinioOptions
{
    public const string SecaoConfiguracao = "Minio";

    public string Endpoint { get; set; } = "localhost:9100";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "icarus-fotos-perfil";
    public bool UsarSsl { get; set; }
}
