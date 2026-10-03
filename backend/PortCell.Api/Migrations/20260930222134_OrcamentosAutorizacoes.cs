using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class OrcamentosAutorizacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Diagnosticos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TecnicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnosticos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diagnosticos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroVersao = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Condicoes = table.Column<string>(type: "text", nullable: true),
                    ValidoAte = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orcamentos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Autorizacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrcamentoVersaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Canal = table.Column<string>(type: "text", nullable: false),
                    Aprovado = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Evidencia = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autorizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Orcamentos_OrcamentoVersaoId",
                        column: x => x.OrcamentoVersaoId,
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrcamentoItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrcamentoVersaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrcamentoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrcamentoItens_Orcamentos_OrcamentoVersaoId",
                        column: x => x.OrcamentoVersaoId,
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacoesAutorizacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrcamentoVersaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UtilizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesAutorizacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesAutorizacao_Orcamentos_OrcamentoVersaoId",
                        column: x => x.OrcamentoVersaoId,
                        principalTable: "Orcamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_OrcamentoVersaoId",
                table: "Autorizacoes",
                column: "OrcamentoVersaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_OrdemServicoId_CreatedAt",
                table: "Autorizacoes",
                columns: new[] { "OrdemServicoId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Diagnosticos_OrdemServicoId",
                table: "Diagnosticos",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrcamentoItens_OrcamentoVersaoId",
                table: "OrcamentoItens",
                column: "OrcamentoVersaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_OrdemServicoId_NumeroVersao",
                table: "Orcamentos",
                columns: new[] { "OrdemServicoId", "NumeroVersao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAutorizacao_OrcamentoVersaoId",
                table: "SolicitacoesAutorizacao",
                column: "OrcamentoVersaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAutorizacao_TokenHash",
                table: "SolicitacoesAutorizacao",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Autorizacoes");

            migrationBuilder.DropTable(
                name: "Diagnosticos");

            migrationBuilder.DropTable(
                name: "OrcamentoItens");

            migrationBuilder.DropTable(
                name: "SolicitacoesAutorizacao");

            migrationBuilder.DropTable(
                name: "Orcamentos");
        }
    }
}
