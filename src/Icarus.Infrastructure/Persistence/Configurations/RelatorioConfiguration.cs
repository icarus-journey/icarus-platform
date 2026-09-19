using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class RelatorioConfiguration : IEntityTypeConfiguration<Relatorio>
{
    public void Configure(EntityTypeBuilder<Relatorio> builder)
    {
        builder.ToTable("relatorio");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Conteudo)
            .IsRequired()
            .HasColumnType("jsonb");

        // Propositalmente sem FK para Missao/Diario/Rotina/Campanha (relatório é snapshot consolidado, seção 14 do DER).
        builder.HasOne(r => r.Usuario)
            .WithMany(u => u.Relatorios)
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.UsuarioId, r.Tipo, r.PeriodoInicio, r.PeriodoFim });
    }
}
