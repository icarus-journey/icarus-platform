using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarLojaDeRecompensas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_item_usuario_id",
                table: "item");

            migrationBuilder.AddColumn<string>(
                name: "estado",
                table: "item",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Disponivel");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "primeiro_resgate_em",
                table: "item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "resgate_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    operacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    custo = table.Column<int>(type: "integer", nullable: false),
                    protecao = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resgatado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estornado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    utilizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resgate_item", x => x.id);
                    table.CheckConstraint("ck_resgate_custo", "custo BETWEEN 1 AND 1000000");
                    table.ForeignKey(
                        name: "fk_resgate_item_item_item_id",
                        column: x => x.item_id,
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_resgate_item_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_usuario_id",
                table: "item",
                column: "usuario_id",
                unique: true,
                filter: "controla_app = 'Sim'");

            migrationBuilder.CreateIndex(
                name: "ix_resgate_item_item_id",
                table: "resgate_item",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_resgate_item_usuario_id_estado",
                table: "resgate_item",
                columns: new[] { "usuario_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_resgate_item_usuario_id_operacao_id",
                table: "resgate_item",
                columns: new[] { "usuario_id", "operacao_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resgate_item");

            migrationBuilder.DropIndex(
                name: "ix_item_usuario_id",
                table: "item");

            migrationBuilder.DropColumn(
                name: "estado",
                table: "item");

            migrationBuilder.DropColumn(
                name: "primeiro_resgate_em",
                table: "item");

            migrationBuilder.CreateIndex(
                name: "ix_item_usuario_id",
                table: "item",
                column: "usuario_id");
        }
    }
}
