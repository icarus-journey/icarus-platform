using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Icarus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriarModeloInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    saldo_pontos = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => x.id);
                    table.CheckConstraint("ck_usuario_saldo_pontos_nao_negativo", "saldo_pontos >= 0");
                });

            migrationBuilder.CreateTable(
                name: "diario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_registro = table.Column<DateOnly>(type: "date", nullable: false),
                    texto = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diario", x => x.id);
                    table.ForeignKey(
                        name: "fk_diario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "epico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    area_da_vida = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    priorizacao = table.Column<int>(type: "integer", nullable: false),
                    cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    prazo_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    prazo_fim = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_epico", x => x.id);
                    table.ForeignKey(
                        name: "fk_epico_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    valor = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    controla_app = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item", x => x.id);
                    table.CheckConstraint("ck_item_valor_nao_negativo", "valor >= 0");
                    table.ForeignKey(
                        name: "fk_item_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "relatorio",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    periodo_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    periodo_fim = table.Column<DateOnly>(type: "date", nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    texto = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relatorio", x => x.id);
                    table.ForeignKey(
                        name: "fk_relatorio_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rotina",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    horas_livres = table.Column<int>(type: "integer", nullable: false),
                    horas_ocupado = table.Column<int>(type: "integer", nullable: false),
                    area_trabalho = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    modalidade_trabalho = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vigencia_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    vigencia_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rotina", x => x.id);
                    table.ForeignKey(
                        name: "fk_rotina_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campanha",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    epico_id = table.Column<long>(type: "bigint", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priorizacao = table.Column<int>(type: "integer", nullable: false),
                    cor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    prazo_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    prazo_fim = table.Column<DateOnly>(type: "date", nullable: false),
                    valor_mensurado = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    unidade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    valor_atual = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campanha", x => x.id);
                    table.ForeignKey(
                        name: "fk_campanha_epicos_epico_id",
                        column: x => x.epico_id,
                        principalTable: "epico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventario_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventario_item", x => x.id);
                    table.CheckConstraint("ck_inventario_item_quantidade_nao_negativa", "quantidade >= 0");
                    table.ForeignKey(
                        name: "fk_inventario_item_itens_item_id",
                        column: x => x.item_id,
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventario_item_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "missao",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campanha_id = table.Column<long>(type: "bigint", nullable: true),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    pontuacao = table.Column<int>(type: "integer", nullable: false),
                    beneficio = table.Column<string>(type: "text", nullable: true),
                    data_limite = table.Column<DateOnly>(type: "date", nullable: true),
                    horario = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    concluida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_missao", x => x.id);
                    table.CheckConstraint("ck_missao_pontuacao_nao_negativa", "pontuacao >= 0");
                    table.ForeignKey(
                        name: "fk_missao_campanha_campanha_id",
                        column: x => x.campanha_id,
                        principalTable: "campanha",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_missao_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "missao_ia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campanha_id = table.Column<long>(type: "bigint", nullable: true),
                    missao_id = table.Column<long>(type: "bigint", nullable: true),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    pontuacao = table.Column<int>(type: "integer", nullable: false),
                    beneficio = table.Column<string>(type: "text", nullable: true),
                    data_limite = table.Column<DateOnly>(type: "date", nullable: true),
                    horario = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_aprovacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    concluida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_missao_ia", x => x.id);
                    table.CheckConstraint("ck_missao_ia_pontuacao_nao_negativa", "pontuacao >= 0");
                    table.ForeignKey(
                        name: "fk_missao_ia_campanha_campanha_id",
                        column: x => x.campanha_id,
                        principalTable: "campanha",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_missao_ia_missao_missao_id",
                        column: x => x.missao_id,
                        principalTable: "missao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_missao_ia_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "movimentacao_pontos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: true),
                    missao_id = table.Column<long>(type: "bigint", nullable: true),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantidade = table.Column<long>(type: "bigint", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimentacao_pontos", x => x.id);
                    table.ForeignKey(
                        name: "fk_movimentacao_pontos_item_item_id",
                        column: x => x.item_id,
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimentacao_pontos_missao_missao_id",
                        column: x => x.missao_id,
                        principalTable: "missao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_movimentacao_pontos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recorrencia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    missao_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    regra = table.Column<string>(type: "jsonb", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recorrencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_recorrencia_missao_missao_id",
                        column: x => x.missao_id,
                        principalTable: "missao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campanha_epico_id",
                table: "campanha",
                column: "epico_id");

            migrationBuilder.CreateIndex(
                name: "ix_diario_usuario_id_data_registro",
                table: "diario",
                columns: new[] { "usuario_id", "data_registro" });

            migrationBuilder.CreateIndex(
                name: "ix_epico_usuario_id",
                table: "epico",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_epico_usuario_id_prazo_fim",
                table: "epico",
                columns: new[] { "usuario_id", "prazo_fim" });

            migrationBuilder.CreateIndex(
                name: "ix_inventario_item_item_id",
                table: "inventario_item",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventario_item_usuario_id_item_id",
                table: "inventario_item",
                columns: new[] { "usuario_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_usuario_id",
                table: "item",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_missao_campanha_id",
                table: "missao",
                column: "campanha_id");

            migrationBuilder.CreateIndex(
                name: "ix_missao_horario",
                table: "missao",
                column: "horario");

            migrationBuilder.CreateIndex(
                name: "ix_missao_status",
                table: "missao",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_missao_usuario_id",
                table: "missao",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_missao_ia_campanha_id",
                table: "missao_ia",
                column: "campanha_id");

            migrationBuilder.CreateIndex(
                name: "ix_missao_ia_missao_id",
                table: "missao_ia",
                column: "missao_id");

            migrationBuilder.CreateIndex(
                name: "ix_missao_ia_status_aprovacao",
                table: "missao_ia",
                column: "status_aprovacao");

            migrationBuilder.CreateIndex(
                name: "ix_missao_ia_usuario_id",
                table: "missao_ia",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_pontos_item_id",
                table: "movimentacao_pontos",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_pontos_missao_id",
                table: "movimentacao_pontos",
                column: "missao_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_pontos_usuario_id_criado_em",
                table: "movimentacao_pontos",
                columns: new[] { "usuario_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "ix_movimentacao_pontos_usuario_id_tipo",
                table: "movimentacao_pontos",
                columns: new[] { "usuario_id", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ix_recorrencia_missao_id",
                table: "recorrencia",
                column: "missao_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_relatorio_usuario_id_tipo_periodo_inicio_periodo_fim",
                table: "relatorio",
                columns: new[] { "usuario_id", "tipo", "periodo_inicio", "periodo_fim" });

            migrationBuilder.CreateIndex(
                name: "ix_rotina_usuario_id_vigencia_inicio_vigencia_fim",
                table: "rotina",
                columns: new[] { "usuario_id", "vigencia_inicio", "vigencia_fim" });

            migrationBuilder.CreateIndex(
                name: "ix_usuario_email",
                table: "usuario",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diario");

            migrationBuilder.DropTable(
                name: "inventario_item");

            migrationBuilder.DropTable(
                name: "missao_ia");

            migrationBuilder.DropTable(
                name: "movimentacao_pontos");

            migrationBuilder.DropTable(
                name: "recorrencia");

            migrationBuilder.DropTable(
                name: "relatorio");

            migrationBuilder.DropTable(
                name: "rotina");

            migrationBuilder.DropTable(
                name: "item");

            migrationBuilder.DropTable(
                name: "missao");

            migrationBuilder.DropTable(
                name: "campanha");

            migrationBuilder.DropTable(
                name: "epico");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
