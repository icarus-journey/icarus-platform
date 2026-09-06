using Icarus.Domain.Common;

namespace Icarus.Domain.Entities;

/// <summary>Quantidade que um usuário possui de um item consumível.</summary>
public class InventarioItem : EntidadeBase
{
    public long Id { get; set; }
    public Guid UsuarioId { get; set; }
    public long ItemId { get; set; }
    public int Quantidade { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
