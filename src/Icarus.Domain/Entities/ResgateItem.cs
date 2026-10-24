using Icarus.Domain.Enums;

namespace Icarus.Domain.Entities;

/// <summary>Uma unidade resgatada, com cópia imutável do custo e dos textos vigentes.</summary>
public class ResgateItem
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public long ItemId { get; set; }
    public Guid OperacaoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int Custo { get; set; }
    public bool Protecao { get; set; }
    public EstadoResgate Estado { get; set; } = EstadoResgate.Resgatado;
    public DateTimeOffset ResgatadoEm { get; set; }
    public DateTimeOffset? EstornadoEm { get; set; }
    public DateTimeOffset? UtilizadoEm { get; set; }
    public Item Item { get; set; } = null!;
}
