namespace Icarus.Domain.Entities;

public sealed class RelatoSuporte
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string ChaveEnvio { get; set; } = string.Empty;
    public Guid ReferenciaCartao { get; set; }
    public string Assunto { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? EnvioTentadoEm { get; set; }
    public string? CartaoId { get; set; }

    public string Protocolo => FormattableString.Invariant($"ICA-{Id:D4}");
}
