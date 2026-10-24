using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDadosDoPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "carga_horaria_estudo_semanal",
                table: "usuario",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "carga_horaria_trabalho_semanal",
                table: "usuario",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "estuda",
                table: "usuario",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fuso_horario",
                table: "usuario",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "trabalha",
                table: "usuario",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_usuario_carga_estudo",
                table: "usuario",
                sql: "carga_horaria_estudo_semanal IS NULL OR (estuda IS TRUE AND carga_horaria_estudo_semanal BETWEEN 0 AND 168)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_usuario_carga_trabalho",
                table: "usuario",
                sql: "carga_horaria_trabalho_semanal IS NULL OR (trabalha IS TRUE AND carga_horaria_trabalho_semanal BETWEEN 0 AND 168)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_usuario_carga_estudo",
                table: "usuario");

            migrationBuilder.DropCheckConstraint(
                name: "ck_usuario_carga_trabalho",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "carga_horaria_estudo_semanal",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "carga_horaria_trabalho_semanal",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "estuda",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "fuso_horario",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "trabalha",
                table: "usuario");
        }
    }
}
