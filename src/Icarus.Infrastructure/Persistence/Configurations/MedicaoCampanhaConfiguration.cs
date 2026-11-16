using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public sealed class MedicaoCampanhaConfiguration : IEntityTypeConfiguration<MedicaoCampanha>
{
    public void Configure(EntityTypeBuilder<MedicaoCampanha> builder)
    {
        builder.ToTable("medicao_campanha", t => t.HasCheckConstraint("ck_medicao_campanha_valores", "valor_atual >= 0 AND valor_mensurado > 0 AND (valor_anterior IS NULL OR valor_anterior >= 0)"));
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ValorAnterior).HasColumnType("numeric(12,2)");
        builder.Property(m => m.ValorAtual).HasColumnType("numeric(12,2)");
        builder.Property(m => m.ValorMensurado).HasColumnType("numeric(12,2)");
        builder.Property(m => m.Unidade).IsRequired().HasMaxLength(30);
        builder.Property(m => m.FusoHorario).HasMaxLength(100);
        builder.HasOne(m => m.Campanha).WithMany().HasForeignKey(m => m.CampanhaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.CampanhaId, m.OperacaoId }).IsUnique().HasDatabaseName("uq_medicao_campanha_operacao");
        builder.HasIndex(m => new { m.CampanhaId, m.RegistradaEm, m.Id });
    }
}
