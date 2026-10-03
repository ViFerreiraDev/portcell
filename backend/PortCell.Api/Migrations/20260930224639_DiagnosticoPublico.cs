using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class DiagnosticoPublico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CausaProvavel",
                table: "Diagnosticos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacoesInternas",
                table: "Diagnosticos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recomendacao",
                table: "Diagnosticos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumoCliente",
                table: "Diagnosticos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CausaProvavel",
                table: "Diagnosticos");

            migrationBuilder.DropColumn(
                name: "ObservacoesInternas",
                table: "Diagnosticos");

            migrationBuilder.DropColumn(
                name: "Recomendacao",
                table: "Diagnosticos");

            migrationBuilder.DropColumn(
                name: "ResumoCliente",
                table: "Diagnosticos");
        }
    }
}
