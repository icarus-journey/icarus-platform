using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistirOcorrencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_movimentacao_pontos_missao_missao_id",
                table: "movimentacao_pontos");

            migrationBuilder.RenameColumn(
                name: "missao_id",
                table: "movimentacao_pontos",
                newName: "ocorrencia_id");

            migrationBuilder.RenameIndex(
                name: "ix_movimentacao_pontos_missao_id",
                table: "movimentacao_pontos",
                newName: "ix_movimentacao_pontos_ocorrencia_id");

            migrationBuilder.CreateTable(
                name: "ocorrencia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    missao_id = table.Column<long>(type: "bigint", nullable: false),
                    data_prevista = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    realizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    concluida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ocorrencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_ocorrencia_missao_missao_id",
                        column: x => x.missao_id,
                        principalTable: "missao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencia_missao_id_data_prevista",
                table: "ocorrencia",
                columns: new[] { "missao_id", "data_prevista" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencia_status",
                table: "ocorrencia",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "fk_movimentacao_pontos_ocorrencias_ocorrencia_id",
                table: "movimentacao_pontos",
                column: "ocorrencia_id",
                principalTable: "ocorrencia",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_movimentacao_pontos_ocorrencias_ocorrencia_id",
                table: "movimentacao_pontos");

            migrationBuilder.DropTable(
                name: "ocorrencia");

            migrationBuilder.RenameColumn(
                name: "ocorrencia_id",
                table: "movimentacao_pontos",
                newName: "missao_id");

            migrationBuilder.RenameIndex(
                name: "ix_movimentacao_pontos_ocorrencia_id",
                table: "movimentacao_pontos",
                newName: "ix_movimentacao_pontos_missao_id");

            migrationBuilder.AddForeignKey(
                name: "fk_movimentacao_pontos_missao_missao_id",
                table: "movimentacao_pontos",
                column: "missao_id",
                principalTable: "missao",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
