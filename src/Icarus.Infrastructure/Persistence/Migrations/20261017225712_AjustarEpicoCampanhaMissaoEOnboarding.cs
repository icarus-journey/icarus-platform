using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjustarEpicoCampanhaMissaoEOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "priorizacao",
                table: "epico",
                newName: "pontuacao");

            migrationBuilder.RenameColumn(
                name: "cor",
                table: "epico",
                newName: "dificuldade");

            migrationBuilder.RenameColumn(
                name: "priorizacao",
                table: "campanha",
                newName: "pontuacao");

            migrationBuilder.RenameColumn(
                name: "cor",
                table: "campanha",
                newName: "dificuldade");

            migrationBuilder.AddColumn<string>(
                name: "dificuldade",
                table: "missao",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "area_da_vida",
                table: "epico",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "area_da_vida",
                table: "campanha",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "trabalho_onboarding",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    epico_id = table.Column<long>(type: "bigint", nullable: true),
                    erro_mensagem = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trabalho_onboarding", x => x.id);
                    table.ForeignKey(
                        name: "fk_trabalho_onboarding_epico_epico_id",
                        column: x => x.epico_id,
                        principalTable: "epico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_trabalho_onboarding_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_epico_pontuacao_nao_negativa",
                table: "epico",
                sql: "pontuacao >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_campanha_pontuacao_nao_negativa",
                table: "campanha",
                sql: "pontuacao >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_trabalho_onboarding_epico_id",
                table: "trabalho_onboarding",
                column: "epico_id");

            migrationBuilder.CreateIndex(
                name: "ix_trabalho_onboarding_usuario_id",
                table: "trabalho_onboarding",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trabalho_onboarding");

            migrationBuilder.DropCheckConstraint(
                name: "ck_epico_pontuacao_nao_negativa",
                table: "epico");

            migrationBuilder.DropCheckConstraint(
                name: "ck_campanha_pontuacao_nao_negativa",
                table: "campanha");

            migrationBuilder.DropColumn(
                name: "dificuldade",
                table: "missao");

            migrationBuilder.DropColumn(
                name: "area_da_vida",
                table: "campanha");

            migrationBuilder.RenameColumn(
                name: "pontuacao",
                table: "epico",
                newName: "priorizacao");

            migrationBuilder.RenameColumn(
                name: "dificuldade",
                table: "epico",
                newName: "cor");

            migrationBuilder.RenameColumn(
                name: "pontuacao",
                table: "campanha",
                newName: "priorizacao");

            migrationBuilder.RenameColumn(
                name: "dificuldade",
                table: "campanha",
                newName: "cor");

            migrationBuilder.AlterColumn<string>(
                name: "area_da_vida",
                table: "epico",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
