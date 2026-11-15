namespace Icarus.Domain.Entities;

/// <summary>Um uso irreversível; a compensação gera outra unidade sem apagar este registro.</summary>
public sealed class ConsumoProtecao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public DateOnly Data { get; set; }
    public string FusoHorario { get; set; } = string.Empty;
    public long ResgateId { get; set; }
    public ResgateItem Resgate { get; set; } = null!;
    public DateTimeOffset ConsumidoEm { get; set; }
    public DateTimeOffset? CompensadoEm { get; set; }
    public long? DevolucaoId { get; set; }
    public ResgateItem? Devolucao { get; set; }
}
