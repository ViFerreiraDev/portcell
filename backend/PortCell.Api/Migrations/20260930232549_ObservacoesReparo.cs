using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class ObservacoesReparo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Reparos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Reparos");
        }
    }
}
