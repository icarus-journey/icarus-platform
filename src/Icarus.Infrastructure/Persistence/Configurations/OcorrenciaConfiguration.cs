using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class OcorrenciaConfiguration : IEntityTypeConfiguration<Ocorrencia>
{
    public void Configure(EntityTypeBuilder<Ocorrencia> builder)
    {
        builder.ToTable("ocorrencia");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(25);

        builder.HasOne(o => o.Missao)
            .WithMany(m => m.Ocorrencias)
            .HasForeignKey(o => o.MissaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.MissaoId, o.DataPrevista })
            .IsUnique();

        builder.HasIndex(o => o.Status);
    }
}
