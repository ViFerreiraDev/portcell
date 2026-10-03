using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class TestesConfiguraveis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TesteItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TesteItens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TesteRespostas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TesteFinalId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNome = table.Column<string>(type: "text", nullable: false),
                    Resultado = table.Column<string>(type: "text", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TesteRespostas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TesteRespostas_TestesFinais_TesteFinalId",
                        column: x => x.TesteFinalId,
                        principalTable: "TestesFinais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TesteItens_EmpresaId_Ordem",
                table: "TesteItens",
                columns: new[] { "EmpresaId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_TesteRespostas_TesteFinalId_TemplateItemId",
                table: "TesteRespostas",
                columns: new[] { "TesteFinalId", "TemplateItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TesteItens");

            migrationBuilder.DropTable(
                name: "TesteRespostas");
        }
    }
}
