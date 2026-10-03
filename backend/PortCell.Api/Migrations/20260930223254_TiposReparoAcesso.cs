using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class TiposReparoAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcessoCriptografado",
                table: "OrdensServico",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcessoNecessario",
                table: "OrdensServico",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TiposReparo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    RequerDesbloqueio = table.Column<bool>(type: "boolean", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposReparo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OsTiposReparo",
                columns: table => new
                {
                    OrdemServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoReparoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsTiposReparo", x => new { x.OrdemServicoId, x.TipoReparoId });
                    table.ForeignKey(
                        name: "FK_OsTiposReparo_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OsTiposReparo_TiposReparo_TipoReparoId",
                        column: x => x.TipoReparoId,
                        principalTable: "TiposReparo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OsTiposReparo_TipoReparoId",
                table: "OsTiposReparo",
                column: "TipoReparoId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposReparo_EmpresaId_Nome",
                table: "TiposReparo",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OsTiposReparo");

            migrationBuilder.DropTable(
                name: "TiposReparo");

            migrationBuilder.DropColumn(
                name: "AcessoCriptografado",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "AcessoNecessario",
                table: "OrdensServico");
        }
    }
}
