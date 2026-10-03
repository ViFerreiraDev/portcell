using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChecklistConfiguravel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Opcoes",
                table: "ChecklistItens",
                type: "text[]",
                nullable: false,
                defaultValue: new[] { "OK", "Avariado", "NaoTestado", "NaoAplicavel" });

            migrationBuilder.AddColumn<bool>(
                name: "PermiteObservacao",
                table: "ChecklistItens",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Opcoes",
                table: "ChecklistItens");

            migrationBuilder.DropColumn(
                name: "PermiteObservacao",
                table: "ChecklistItens");
        }
    }
}
