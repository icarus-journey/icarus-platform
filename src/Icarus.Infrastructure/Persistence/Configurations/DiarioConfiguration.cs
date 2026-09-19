using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class DiarioConfiguration : IEntityTypeConfiguration<Diario>
{
    public void Configure(EntityTypeBuilder<Diario> builder)
    {
        builder.ToTable("diario");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Texto)
            .IsRequired();

        builder.HasOne(d => d.Usuario)
            .WithMany(u => u.Diarios)
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sem UNIQUE por enquanto: "mais de uma entrada por dia" ainda é pendência de produto (seção 37 do DER).
        builder.HasIndex(d => new { d.UsuarioId, d.DataRegistro });
    }
}
