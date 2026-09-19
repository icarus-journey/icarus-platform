using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class RecorrenciaConfiguration : IEntityTypeConfiguration<Recorrencia>
{
    public void Configure(EntityTypeBuilder<Recorrencia> builder)
    {
        builder.ToTable("recorrencia");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Tipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Regra)
            .IsRequired()
            .HasColumnType("jsonb");

        // Relação 1:0..1 — no máximo uma recorrência por missão (ver seção 12 do DER).
        builder.HasOne(r => r.Missao)
            .WithOne(m => m.Recorrencia)
            .HasForeignKey<Recorrencia>(r => r.MissaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.MissaoId)
            .IsUnique();
    }
}
