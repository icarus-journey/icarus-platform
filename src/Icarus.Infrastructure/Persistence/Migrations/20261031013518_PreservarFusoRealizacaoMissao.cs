using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreservarFusoRealizacaoMissao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fuso_horario_realizacao",
                table: "ocorrencia",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fuso_horario_realizacao",
                table: "ocorrencia");
        }
    }
}
