using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAutenticacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "senha_hash",
                table: "usuario",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "token_renovacao",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revogado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    substituido_por_id = table.Column<long>(type: "bigint", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token_renovacao", x => x.id);
                    table.ForeignKey(
                        name: "fk_token_renovacao_token_renovacao_substituido_por_id",
                        column: x => x.substituido_por_id,
                        principalTable: "token_renovacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_token_renovacao_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_token_renovacao_substituido_por_id",
                table: "token_renovacao",
                column: "substituido_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_token_renovacao_token_hash",
                table: "token_renovacao",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_token_renovacao_usuario_id_expira_em",
                table: "token_renovacao",
                columns: new[] { "usuario_id", "expira_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "token_renovacao");

            migrationBuilder.DropColumn(
                name: "senha_hash",
                table: "usuario");
        }
    }
}
