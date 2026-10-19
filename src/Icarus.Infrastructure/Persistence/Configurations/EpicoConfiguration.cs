using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class EpicoConfiguration : IEntityTypeConfiguration<Epico>
{
    public void Configure(EntityTypeBuilder<Epico> builder)
    {
        builder.ToTable("epico", t => t.HasCheckConstraint("ck_epico_pontuacao_nao_negativa", "pontuacao >= 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Titulo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AreaDaVida)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.Dificuldade)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Estado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(e => e.Usuario)
            .WithMany(u => u.Epicos)
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UsuarioId);
        builder.HasIndex(e => new { e.UsuarioId, e.PrazoFim });
    }
}
