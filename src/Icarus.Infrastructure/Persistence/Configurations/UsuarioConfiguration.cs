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
            t.HasCheckConstraint("ck_usuario_relato_sono", "(sono_habitual_minutos IS NULL AND meta_sono_minutos IS NULL AND sono_informado_em IS NULL) OR (sono_habitual_minutos IS NOT NULL AND sono_habitual_minutos BETWEEN 0 AND 1440 AND sono_informado_em IS NOT NULL AND (meta_sono_minutos IS NULL OR meta_sono_minutos BETWEEN 1 AND 1440))");
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
        builder.Property(u => u.VersaoRelatoSono).IsConcurrencyToken().HasDefaultValue(Guid.Empty);
        builder.Property(u => u.FusoHorario).HasMaxLength(100);
        builder.Property(u => u.ProtecaoAtivadaEm).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(u => u.FusoInicioProtecao).HasMaxLength(100);
        builder.Property(u => u.PlanejamentoVersionadoDesde).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.FotoChave)
            .HasMaxLength(100);
    }
}
