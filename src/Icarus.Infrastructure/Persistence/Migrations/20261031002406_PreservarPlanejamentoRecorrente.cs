using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreservarPlanejamentoRecorrente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "alteracoes_a_partir_de",
                table: "recorrencia",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "planejamento",
                table: "ocorrencia",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alteracoes_a_partir_de",
                table: "recorrencia");

            migrationBuilder.DropColumn(
                name: "planejamento",
                table: "ocorrencia");
        }
    }
}
