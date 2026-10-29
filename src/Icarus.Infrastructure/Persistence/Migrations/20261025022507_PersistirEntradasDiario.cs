using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistirEntradasDiario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "excluida_em",
                table: "diario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "diario",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Reflexao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "excluida_em",
                table: "diario");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "diario");
        }
    }
}
