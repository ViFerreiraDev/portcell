using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class Estornos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EstornoDeId",
                table: "Pagamentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Justificativa",
                table: "Pagamentos",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_EstornoDeId",
                table: "Pagamentos",
                column: "EstornoDeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pagamentos_EstornoDeId",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "EstornoDeId",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "Justificativa",
                table: "Pagamentos");
        }
    }
}
