using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public sealed class OutboxRelatorioConfiguration : IEntityTypeConfiguration<OutboxRelatorio>
{
    public void Configure(EntityTypeBuilder<OutboxRelatorio> builder)
    {
        builder.ToTable("outbox_relatorio"); builder.HasKey(o => o.Id);
        builder.Property(o => o.Carga).HasColumnType("jsonb");
        builder.HasIndex(o => new { o.PublicadaEm, o.ProximaTentativaEm });
        builder.HasOne<TrabalhoRelatorioSemanal>().WithMany().HasForeignKey(o => o.TrabalhoId).OnDelete(DeleteBehavior.Cascade);
    }
}
