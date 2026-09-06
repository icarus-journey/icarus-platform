using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class MissaoConfiguration : IEntityTypeConfiguration<Missao>
{
    public void Configure(EntityTypeBuilder<Missao> builder)
    {
        builder.ToTable("missao", t => t.HasCheckConstraint("ck_missao_pontuacao_nao_negativa", "pontuacao >= 0"));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Titulo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(m => m.Usuario)
            .WithMany(u => u.Missoes)
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // campanha_id nulo representa missão avulsa (ver seção 9 do DER).
        builder.HasOne(m => m.Campanha)
            .WithMany(c => c.Missoes)
            .HasForeignKey(m => m.CampanhaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.UsuarioId);
        builder.HasIndex(m => m.CampanhaId);
        builder.HasIndex(m => m.Horario);
        builder.HasIndex(m => m.Status);
    }
}
