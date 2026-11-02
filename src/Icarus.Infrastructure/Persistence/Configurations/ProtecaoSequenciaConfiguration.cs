using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public sealed class FechamentoJornadaConfiguration : IEntityTypeConfiguration<FechamentoJornada>
{
    public void Configure(EntityTypeBuilder<FechamentoJornada> builder)
    {
        builder.ToTable("fechamento_jornada");
        builder.HasKey(x => new { x.UsuarioId, x.Data });
        builder.Property(x => x.FusoHorario).HasMaxLength(100);
        builder.Property(x => x.Estado).HasMaxLength(24);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ConsumoProtecaoConfiguration : IEntityTypeConfiguration<ConsumoProtecao>
{
    public void Configure(EntityTypeBuilder<ConsumoProtecao> builder)
    {
        builder.ToTable("consumo_protecao");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FusoHorario).HasMaxLength(100);
        builder.HasIndex(x => x.ResgateId).IsUnique();
        builder.HasIndex(x => new { x.UsuarioId, x.Data }).IsUnique().HasFilter("compensado_em IS NULL");
        builder.HasOne(x => x.Resgate).WithMany().HasForeignKey(x => x.ResgateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Devolucao).WithMany().HasForeignKey(x => x.DevolucaoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OutboxJornadaConfiguration : IEntityTypeConfiguration<OutboxJornada>
{
    public void Configure(EntityTypeBuilder<OutboxJornada> builder)
    {
        builder.ToTable("outbox_jornada");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ConcluidaEm, x.ProximaTentativaEm });
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
