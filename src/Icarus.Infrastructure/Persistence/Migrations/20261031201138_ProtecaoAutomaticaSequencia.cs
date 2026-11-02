using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtecaoAutomaticaSequencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fuso_inicio_protecao",
                table: "usuario",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "protecao_ativada_em",
                table: "usuario",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "compensacao_origem_id",
                table: "resgate_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "disponivel_protecao_desde",
                table: "resgate_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "consumo_protecao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    fuso_horario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resgate_id = table.Column<long>(type: "bigint", nullable: false),
                    consumido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    compensado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    devolucao_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consumo_protecao", x => x.id);
                    table.ForeignKey(
                        name: "fk_consumo_protecao_resgate_item_devolucao_id",
                        column: x => x.devolucao_id,
                        principalTable: "resgate_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consumo_protecao_resgate_item_resgate_id",
                        column: x => x.resgate_id,
                        principalTable: "resgate_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_consumo_protecao_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fechamento_jornada",
                columns: table => new
                {
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    fuso_horario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fechamento_jornada", x => new { x.usuario_id, x.data });
                    table.ForeignKey(
                        name: "fk_fechamento_jornada_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbox_jornada",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    proxima_tentativa_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concluida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_jornada", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbox_jornada_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_resgate_item_compensacao_origem_id",
                table: "resgate_item",
                column: "compensacao_origem_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_consumo_protecao_devolucao_id",
                table: "consumo_protecao",
                column: "devolucao_id");

            migrationBuilder.CreateIndex(
                name: "ix_consumo_protecao_resgate_id",
                table: "consumo_protecao",
                column: "resgate_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_consumo_protecao_usuario_id_data",
                table: "consumo_protecao",
                columns: new[] { "usuario_id", "data" },
                unique: true,
                filter: "compensado_em IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_jornada_concluida_em_proxima_tentativa_em",
                table: "outbox_jornada",
                columns: new[] { "concluida_em", "proxima_tentativa_em" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_jornada_usuario_id",
                table: "outbox_jornada",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumo_protecao");

            migrationBuilder.DropTable(
                name: "fechamento_jornada");

            migrationBuilder.DropTable(
                name: "outbox_jornada");

            migrationBuilder.DropIndex(
                name: "ix_resgate_item_compensacao_origem_id",
                table: "resgate_item");

            migrationBuilder.DropColumn(
                name: "fuso_inicio_protecao",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "protecao_ativada_em",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "compensacao_origem_id",
                table: "resgate_item");

            migrationBuilder.DropColumn(
                name: "disponivel_protecao_desde",
                table: "resgate_item");
        }
    }
}
