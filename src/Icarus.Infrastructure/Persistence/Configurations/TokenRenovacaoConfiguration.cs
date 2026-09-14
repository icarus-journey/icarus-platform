using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class TokenRenovacaoConfiguration : IEntityTypeConfiguration<TokenRenovacao>
{
    public void Configure(EntityTypeBuilder<TokenRenovacao> builder)
    {
        builder.ToTable("token_renovacao");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasOne(t => t.Usuario)
            .WithMany(u => u.TokensRenovacao)
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: o token antigo nunca é apagado quando substituído, só marcado (rotação/auditoria).
        builder.HasOne(t => t.SubstituidoPor)
            .WithMany()
            .HasForeignKey(t => t.SubstituidoPorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => new { t.UsuarioId, t.ExpiraEm });
    }
}
