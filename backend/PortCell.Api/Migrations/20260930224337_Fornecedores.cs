using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class Fornecedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Compatibilidade",
                table: "Pecas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentoCompra",
                table: "MovimentosEstoque",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FornecedorId",
                table: "MovimentosEstoque",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Contato = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_FornecedorId",
                table: "MovimentosEstoque",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_EmpresaId_Nome",
                table: "Fornecedores",
                columns: new[] { "EmpresaId", "Nome" });

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentosEstoque_Fornecedores_FornecedorId",
                table: "MovimentosEstoque",
                column: "FornecedorId",
                principalTable: "Fornecedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimentosEstoque_Fornecedores_FornecedorId",
                table: "MovimentosEstoque");

            migrationBuilder.DropTable(
                name: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosEstoque_FornecedorId",
                table: "MovimentosEstoque");

            migrationBuilder.DropColumn(
                name: "Compatibilidade",
                table: "Pecas");

            migrationBuilder.DropColumn(
                name: "DocumentoCompra",
                table: "MovimentosEstoque");

            migrationBuilder.DropColumn(
                name: "FornecedorId",
                table: "MovimentosEstoque");
        }
    }
}
