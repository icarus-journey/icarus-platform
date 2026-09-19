using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class MissaoIAConfiguration : IEntityTypeConfiguration<MissaoIA>
{
    public void Configure(EntityTypeBuilder<MissaoIA> builder)
    {
        builder.ToTable("missao_ia", t => t.HasCheckConstraint("ck_missao_ia_pontuacao_nao_negativa", "pontuacao >= 0"));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Titulo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.StatusAprovacao)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(m => m.Usuario)
            .WithMany(u => u.MissoesIA)
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // campanha_id nulo representa sugestão avulsa (mesma regra de Missao — seção 9 do DER).
        builder.HasOne(m => m.Campanha)
            .WithMany(c => c.MissoesIA)
            .HasForeignKey(m => m.CampanhaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        // missao_id só é preenchido quando a sugestão é aprovada e a missão real é criada.
        builder.HasOne(m => m.Missao)
            .WithMany(mi => mi.MissoesIA)
            .HasForeignKey(m => m.MissaoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.UsuarioId);
        builder.HasIndex(m => m.StatusAprovacao);
    }
}
