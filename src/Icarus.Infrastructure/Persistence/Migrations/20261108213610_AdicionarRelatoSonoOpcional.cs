using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRelatoSonoOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "meta_sono_minutos",
                table: "usuario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sono_habitual_minutos",
                table: "usuario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sono_informado_em",
                table: "usuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "versao_relato_sono",
                table: "usuario",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddCheckConstraint(
                name: "ck_usuario_relato_sono",
                table: "usuario",
                sql: "(sono_habitual_minutos IS NULL AND meta_sono_minutos IS NULL AND sono_informado_em IS NULL) OR (sono_habitual_minutos IS NOT NULL AND sono_habitual_minutos BETWEEN 0 AND 1440 AND sono_informado_em IS NOT NULL AND (meta_sono_minutos IS NULL OR meta_sono_minutos BETWEEN 1 AND 1440))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_usuario_relato_sono",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "meta_sono_minutos",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "sono_habitual_minutos",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "sono_informado_em",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "versao_relato_sono",
                table: "usuario");
        }
    }
}
