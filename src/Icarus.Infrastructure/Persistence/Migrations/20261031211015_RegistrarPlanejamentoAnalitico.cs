using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarPlanejamentoAnalitico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "planejamento_versionado_desde",
                table: "usuario",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "planejamento_encerrado_em",
                table: "missao",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "registro_planejamento_ocorrencia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ocorrencia_id = table.Column<long>(type: "bigint", nullable: false),
                    data_prevista = table.Column<DateOnly>(type: "date", nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registro_planejamento_ocorrencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_registro_planejamento_ocorrencia_ocorrencia_ocorrencia_id",
                        column: x => x.ocorrencia_id,
                        principalTable: "ocorrencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registro_planejamento_ocorrencia_ocorrencia_id_registrado_e",
                table: "registro_planejamento_ocorrencia",
                columns: new[] { "ocorrencia_id", "registrado_em", "id" });
            // Fotografia observada agora: não atribui uma data antiga às versões do legado.
            migrationBuilder.Sql("""
                INSERT INTO registro_planejamento_ocorrencia (ocorrencia_id, data_prevista, ativa, registrado_em)
                SELECT id, data_prevista, status <> 'Excluida', CURRENT_TIMESTAMP FROM ocorrencia;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registro_planejamento_ocorrencia");

            migrationBuilder.DropColumn(
                name: "planejamento_versionado_desde",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "planejamento_encerrado_em",
                table: "missao");
        }
    }
}
