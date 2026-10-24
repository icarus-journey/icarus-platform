using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public sealed class InboxRelatorioConfiguration : IEntityTypeConfiguration<InboxRelatorio>
{
    public void Configure(EntityTypeBuilder<InboxRelatorio> builder)
    {
        builder.ToTable("inbox_relatorio"); builder.HasKey(i => i.Id);
        builder.Property(i => i.Estado).HasMaxLength(20).IsRequired();
        builder.HasOne<TrabalhoRelatorioSemanal>().WithMany().HasForeignKey(i => i.TrabalhoId).OnDelete(DeleteBehavior.Cascade);
    }
}
