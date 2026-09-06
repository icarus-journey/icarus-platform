using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class MovimentacaoPontosConfiguration : IEntityTypeConfiguration<MovimentacaoPontos>
{
    public void Configure(EntityTypeBuilder<MovimentacaoPontos> builder)
    {
        builder.ToTable("movimentacao_pontos");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.CriadoEm)
            .IsRequired();

        builder.HasOne(m => m.Usuario)
            .WithMany(u => u.MovimentacoesPontos)
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict propositalmente: excluir um item/missão não pode apagar o histórico de pontos (ver seção 18 do DER).
        builder.HasOne(m => m.Item)
            .WithMany(i => i.MovimentacoesPontos)
            .HasForeignKey(m => m.ItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Missao)
            .WithMany(mi => mi.MovimentacoesPontos)
            .HasForeignKey(m => m.MissaoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.UsuarioId, m.CriadoEm });
        builder.HasIndex(m => new { m.UsuarioId, m.Tipo });
    }
}
