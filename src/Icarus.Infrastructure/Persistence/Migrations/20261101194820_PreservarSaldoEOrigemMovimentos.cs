using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreservarSaldoEOrigemMovimentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_movimentacao_pontos_usuario_id_criado_em",
                table: "movimentacao_pontos");

            migrationBuilder.AddColumn<long>(
                name: "saldo_resultante",
                table: "movimentacao_pontos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "titulo_origem",
                table: "movimentacao_pontos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_pontos_usuario_id_criado_em_id",
                table: "movimentacao_pontos",
                columns: new[] { "usuario_id", "criado_em", "id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_movimentacao_pontos_usuario_id_criado_em_id",
                table: "movimentacao_pontos");

            migrationBuilder.DropColumn(
                name: "saldo_resultante",
                table: "movimentacao_pontos");

            migrationBuilder.DropColumn(
                name: "titulo_origem",
                table: "movimentacao_pontos");

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_pontos_usuario_id_criado_em",
                table: "movimentacao_pontos",
                columns: new[] { "usuario_id", "criado_em" });
        }
    }
}
