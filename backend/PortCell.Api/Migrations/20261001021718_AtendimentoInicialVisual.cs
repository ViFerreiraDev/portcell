using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class AtendimentoInicialVisual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AtendimentoDireto",
                table: "OrdensServico",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PecaId",
                table: "OrcamentoItens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PontosAvaria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Face = table.Column<string>(type: "text", nullable: false),
                    X = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Y = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontosAvaria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PontosAvaria_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrcamentoItens_PecaId",
                table: "OrcamentoItens",
                column: "PecaId");

            migrationBuilder.CreateIndex(
                name: "IX_PontosAvaria_OrdemServicoId",
                table: "PontosAvaria",
                column: "OrdemServicoId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrcamentoItens_Pecas_PecaId",
                table: "OrcamentoItens",
                column: "PecaId",
                principalTable: "Pecas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrcamentoItens_Pecas_PecaId",
                table: "OrcamentoItens");

            migrationBuilder.DropTable(
                name: "PontosAvaria");

            migrationBuilder.DropIndex(
                name: "IX_OrcamentoItens_PecaId",
                table: "OrcamentoItens");

            migrationBuilder.DropColumn(
                name: "AtendimentoDireto",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "PecaId",
                table: "OrcamentoItens");
        }
    }
}
