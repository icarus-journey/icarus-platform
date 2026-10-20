using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icarus.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario", t =>
        {
            t.HasCheckConstraint("ck_usuario_saldo_pontos_nao_negativo", "saldo_pontos >= 0");
            t.HasCheckConstraint("ck_usuario_carga_estudo",
                "carga_horaria_estudo_semanal IS NULL OR (estuda IS TRUE AND carga_horaria_estudo_semanal BETWEEN 0 AND 168)");
            t.HasCheckConstraint("ck_usuario_carga_trabalho",
                "carga_horaria_trabalho_semanal IS NULL OR (trabalha IS TRUE AND carga_horaria_trabalho_semanal BETWEEN 0 AND 168)");
        });

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(u => u.SenhaHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.SaldoPontos)
            .IsRequired()
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.Property(u => u.CargaHorariaEstudoSemanal).HasPrecision(5, 2);
        builder.Property(u => u.CargaHorariaTrabalhoSemanal).HasPrecision(5, 2);
        builder.Property(u => u.FusoHorario).HasMaxLength(100);
    }
}
