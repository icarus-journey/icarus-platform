using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class InventarioItemConfiguration : IEntityTypeConfiguration<InventarioItem>
{
    public void Configure(EntityTypeBuilder<InventarioItem> builder)
    {
        builder.ToTable("inventario_item", t => t.HasCheckConstraint("ck_inventario_item_quantidade_nao_negativa", "quantidade >= 0"));

        builder.HasKey(ii => ii.Id);

        builder.HasOne(ii => ii.Usuario)
            .WithMany(u => u.InventarioItens)
            .HasForeignKey(ii => ii.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ii => ii.Item)
            .WithMany(i => i.InventarioItens)
            .HasForeignKey(ii => ii.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Uma única linha por item no inventário de cada usuário (seção 26 do DER).
        builder.HasIndex(ii => new { ii.UsuarioId, ii.ItemId })
            .IsUnique();
    }
}
