using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class PrazoHashOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConteudoHash",
                table: "Orcamentos",
                type: "text",
                nullable: false,
                defaultValue: "LEGADO_SEM_HASH");

            migrationBuilder.AddColumn<string>(
                name: "PrazoEstimado",
                table: "Orcamentos",
                type: "text",
                nullable: false,
                defaultValue: "Prazo não informado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConteudoHash",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "PrazoEstimado",
                table: "Orcamentos");
        }
    }
}
