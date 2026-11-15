using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public sealed class RegistroPlanejamentoOcorrenciaConfiguration : IEntityTypeConfiguration<RegistroPlanejamentoOcorrencia>
{
    public void Configure(EntityTypeBuilder<RegistroPlanejamentoOcorrencia> builder)
    {
        builder.ToTable("registro_planejamento_ocorrencia");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OcorrenciaId, x.RegistradoEm, x.Id });
        builder.HasOne(x => x.Ocorrencia).WithMany().HasForeignKey(x => x.OcorrenciaId).OnDelete(DeleteBehavior.Cascade);
    }
}
