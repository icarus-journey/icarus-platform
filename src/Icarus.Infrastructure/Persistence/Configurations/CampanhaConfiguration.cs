using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class CampanhaConfiguration : IEntityTypeConfiguration<Campanha>
{
    public void Configure(EntityTypeBuilder<Campanha> builder)
    {
        builder.ToTable("campanha");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Titulo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Cor)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Unidade)
            .HasMaxLength(30);

        builder.Property(c => c.ValorMensurado)
            .HasColumnType("numeric(12,2)");

        builder.Property(c => c.ValorAtual)
            .HasColumnType("numeric(12,2)");

        builder.Property(c => c.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(c => c.Epico)
            .WithMany(e => e.Campanhas)
            .HasForeignKey(c => c.EpicoId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.EpicoId);
    }
}
