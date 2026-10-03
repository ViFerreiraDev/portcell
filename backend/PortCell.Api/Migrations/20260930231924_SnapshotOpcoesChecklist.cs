using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotOpcoesChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "ItemOpcoes",
                table: "ChecklistRespostas",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<bool>(
                name: "PermiteObservacao",
                table: "ChecklistRespostas",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemOpcoes",
                table: "ChecklistRespostas");

            migrationBuilder.DropColumn(
                name: "PermiteObservacao",
                table: "ChecklistRespostas");
        }
    }
}
