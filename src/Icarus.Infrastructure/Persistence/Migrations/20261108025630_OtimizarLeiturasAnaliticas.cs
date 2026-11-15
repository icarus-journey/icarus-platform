using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OtimizarLeiturasAnaliticas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_ocorrencia_missao_id_realizada_em",
                table: "ocorrencia",
                columns: new[] { "missao_id", "realizada_em" },
                filter: "realizada_em IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ocorrencia_missao_id_realizada_em",
                table: "ocorrencia");
        }
    }
}
