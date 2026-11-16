using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarMedicoesCampanha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medicao_campanha",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    campanha_id = table.Column<long>(type: "bigint", nullable: false),
                    valor_anterior = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    valor_atual = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    valor_mensurado = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    unidade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    registrada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fuso_horario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    operacao_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medicao_campanha", x => x.id);
                    table.CheckConstraint("ck_medicao_campanha_valores", "valor_atual >= 0 AND valor_mensurado > 0 AND (valor_anterior IS NULL OR valor_anterior >= 0)");
                    table.ForeignKey(
                        name: "fk_medicao_campanha_campanha_campanha_id",
                        column: x => x.campanha_id,
                        principalTable: "campanha",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_medicao_campanha_campanha_id_registrada_em_id",
                table: "medicao_campanha",
                columns: new[] { "campanha_id", "registrada_em", "id" });

            migrationBuilder.CreateIndex(
                name: "uq_medicao_campanha_operacao",
                table: "medicao_campanha",
                columns: new[] { "campanha_id", "operacao_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medicao_campanha");
        }
    }
}
