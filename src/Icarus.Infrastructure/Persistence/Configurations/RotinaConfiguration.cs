using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class RotinaConfiguration : IEntityTypeConfiguration<Rotina>
{
    public void Configure(EntityTypeBuilder<Rotina> builder)
    {
        builder.ToTable("rotina");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.AreaTrabalho)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.ModalidadeTrabalho)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(r => r.Usuario)
            .WithMany(u => u.Rotinas)
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ajuda a localizar a rotina vigente em determinado instante; ver seção 6 do DER.
        builder.HasIndex(r => new { r.UsuarioId, r.VigenciaInicio, r.VigenciaFim });
    }
}
