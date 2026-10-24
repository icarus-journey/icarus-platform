using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public sealed class ResgateItemConfiguration : IEntityTypeConfiguration<ResgateItem>
{
    public void Configure(EntityTypeBuilder<ResgateItem> builder)
    {
        builder.ToTable("resgate_item", t => t.HasCheckConstraint("ck_resgate_custo", "custo BETWEEN 1 AND 1000000"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Descricao).HasMaxLength(1000);
        builder.Property(r => r.Estado).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(r => new { r.UsuarioId, r.OperacaoId }).IsUnique();
        builder.HasIndex(r => new { r.UsuarioId, r.Estado });
        builder.HasOne(r => r.Item).WithMany().HasForeignKey(r => r.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
