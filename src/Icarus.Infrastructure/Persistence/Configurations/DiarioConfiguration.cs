using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class DiarioConfiguration : IEntityTypeConfiguration<Diario>
{
    public void Configure(EntityTypeBuilder<Diario> builder)
    {
        builder.ToTable("diario");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Texto)
            .IsRequired();
        builder.Property(d => d.Tipo).HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(Icarus.Domain.Enums.TipoEntradaDiario.Reflexao);
        builder.Property(d => d.AtualizadoEm).IsConcurrencyToken();

        builder.HasOne(d => d.Usuario)
            .WithMany(u => u.Diarios)
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // O histórico da tela permite entradas independentes no mesmo dia.
        builder.HasIndex(d => new { d.UsuarioId, d.DataRegistro });
    }
}
