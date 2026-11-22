using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

internal sealed class RelatoSuporteConfiguration : IEntityTypeConfiguration<RelatoSuporte>
{
    public void Configure(EntityTypeBuilder<RelatoSuporte> builder)
    {
        builder.ToTable("relatos_suporte");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ChaveEnvio).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Assunto).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Descricao).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.CartaoId).HasMaxLength(24);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(r => new { r.UsuarioId, r.ChaveEnvio }).IsUnique();
        builder.HasIndex(r => new { r.UsuarioId, r.CriadoEm, r.Id }).IsDescending(false, true, true);
        builder.HasIndex(r => r.CartaoId).IsUnique();
    }
}
