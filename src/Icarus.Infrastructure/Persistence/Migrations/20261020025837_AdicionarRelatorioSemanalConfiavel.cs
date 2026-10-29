using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRelatorioSemanalConfiavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_relatorio_usuario_id_tipo_periodo_inicio_periodo_fim",
                table: "relatorio");

            migrationBuilder.CreateTable(
                name: "trabalho_relatorio_semanal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    periodo_fim = table.Column<DateOnly>(type: "date", nullable: false),
                    fuso_horario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentativa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    indicadores = table.Column<string>(type: "jsonb", nullable: false),
                    falha_codigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trabalho_relatorio_semanal", x => x.id);
                    table.ForeignKey(
                        name: "fk_trabalho_relatorio_semanal_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbox_relatorio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trabalho_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tentativa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_relatorio", x => x.id);
                    table.ForeignKey(
                        name: "fk_inbox_relatorio_trabalho_relatorio_semanal_trabalho_id",
                        column: x => x.trabalho_id,
                        principalTable: "trabalho_relatorio_semanal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbox_relatorio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trabalho_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carga = table.Column<string>(type: "jsonb", nullable: true),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    proxima_tentativa_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    publicada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_relatorio", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbox_relatorio_trabalho_relatorio_semanal_trabalho_id",
                        column: x => x.trabalho_id,
                        principalTable: "trabalho_relatorio_semanal",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_relatorio_usuario_id_tipo_periodo_inicio_periodo_fim",
                table: "relatorio",
                columns: new[] { "usuario_id", "tipo", "periodo_inicio", "periodo_fim" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inbox_relatorio_trabalho_id",
                table: "inbox_relatorio",
                column: "trabalho_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_relatorio_publicada_em_proxima_tentativa_em",
                table: "outbox_relatorio",
                columns: new[] { "publicada_em", "proxima_tentativa_em" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_relatorio_trabalho_id",
                table: "outbox_relatorio",
                column: "trabalho_id");

            migrationBuilder.CreateIndex(
                name: "ix_trabalho_relatorio_semanal_usuario_id_periodo_inicio_period",
                table: "trabalho_relatorio_semanal",
                columns: new[] { "usuario_id", "periodo_inicio", "periodo_fim" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_relatorio");

            migrationBuilder.DropTable(
                name: "outbox_relatorio");

            migrationBuilder.DropTable(
                name: "trabalho_relatorio_semanal");

            migrationBuilder.DropIndex(
                name: "ix_relatorio_usuario_id_tipo_periodo_inicio_periodo_fim",
                table: "relatorio");

            migrationBuilder.CreateIndex(
                name: "ix_relatorio_usuario_id_tipo_periodo_inicio_periodo_fim",
                table: "relatorio",
                columns: new[] { "usuario_id", "tipo", "periodo_inicio", "periodo_fim" });
        }
    }
}
