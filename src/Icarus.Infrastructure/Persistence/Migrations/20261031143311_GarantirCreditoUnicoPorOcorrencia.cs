using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GarantirCreditoUnicoPorOcorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_movimentacao_ganho_missao_ocorrencia",
                table: "movimentacao_pontos",
                column: "ocorrencia_id",
                unique: true,
                filter: "tipo = 'GanhoMissao' AND ocorrencia_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_movimentacao_ganho_missao_ocorrencia",
                table: "movimentacao_pontos");
        }
    }
}
