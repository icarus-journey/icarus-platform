namespace Icarus.Domain.Common;

/// <summary>
/// Campos de auditoria comuns à maioria das entidades do domínio.
/// Preenchidos automaticamente pelo IcarusDbContext ao salvar.
/// </summary>
public abstract class EntidadeBase
{
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}
