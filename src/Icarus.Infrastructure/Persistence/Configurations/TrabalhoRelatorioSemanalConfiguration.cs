using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public sealed class TrabalhoRelatorioSemanalConfiguration : IEntityTypeConfiguration<TrabalhoRelatorioSemanal>
{
    public void Configure(EntityTypeBuilder<TrabalhoRelatorioSemanal> builder)
    {
        builder.ToTable("trabalho_relatorio_semanal"); builder.HasKey(t => t.Id);
        builder.Property(t => t.FusoHorario).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Estado).HasMaxLength(20).IsRequired();
        builder.Property(t => t.FalhaCodigo).HasMaxLength(80);
        builder.Property(t => t.Indicadores).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(t => new { t.UsuarioId, t.PeriodoInicio, t.PeriodoFim }).IsUnique();
        builder.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
