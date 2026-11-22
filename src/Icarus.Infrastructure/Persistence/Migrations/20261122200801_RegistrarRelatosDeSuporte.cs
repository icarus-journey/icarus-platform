using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarRelatosDeSuporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "relatos_suporte",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chave_envio = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    referencia_cartao = table.Column<Guid>(type: "uuid", nullable: false),
                    assunto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    envio_tentado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cartao_id = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relatos_suporte", x => x.id);
                    table.ForeignKey(
                        name: "fk_relatos_suporte_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_relatos_suporte_cartao_id",
                table: "relatos_suporte",
                column: "cartao_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_relatos_suporte_usuario_id_chave_envio",
                table: "relatos_suporte",
                columns: new[] { "usuario_id", "chave_envio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_relatos_suporte_usuario_id_criado_em_id",
                table: "relatos_suporte",
                columns: new[] { "usuario_id", "criado_em", "id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "relatos_suporte");
        }
    }
}
