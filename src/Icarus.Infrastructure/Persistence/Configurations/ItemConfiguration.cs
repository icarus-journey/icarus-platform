using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("item", t => t.HasCheckConstraint("ck_item_valor_nao_negativo", "valor >= 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.ControlaApp)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        // No modelo atual, itens são criados/administrados pelo próprio usuário (seção 15 do DER).
        builder.HasOne(i => i.Usuario)
            .WithMany(u => u.Itens)
            .HasForeignKey(i => i.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
