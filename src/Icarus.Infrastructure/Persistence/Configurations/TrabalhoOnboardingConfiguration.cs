using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class TrabalhoOnboardingConfiguration : IEntityTypeConfiguration<TrabalhoOnboarding>
{
    public void Configure(EntityTypeBuilder<TrabalhoOnboarding> builder)
    {
        builder.ToTable("trabalho_onboarding");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.ErroMensagem)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Usuario)
            .WithMany(u => u.TrabalhosOnboarding)
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Epico)
            .WithMany()
            .HasForeignKey(t => t.EpicoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.UsuarioId);
    }
}
