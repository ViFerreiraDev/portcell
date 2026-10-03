using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortCell.Api.Migrations
{
    /// <inheritdoc />
    public partial class DadosCadastrais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Capacidade",
                table: "Aparelhos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cor",
                table: "Aparelhos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroSerie",
                table: "Aparelhos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Aparelhos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Aparelhos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Aparelhos_EmpresaId_NumeroSerie",
                table: "Aparelhos",
                columns: new[] { "EmpresaId", "NumeroSerie" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Aparelhos_EmpresaId_NumeroSerie",
                table: "Aparelhos");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Capacidade",
                table: "Aparelhos");

            migrationBuilder.DropColumn(
                name: "Cor",
                table: "Aparelhos");

            migrationBuilder.DropColumn(
                name: "NumeroSerie",
                table: "Aparelhos");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Aparelhos");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Aparelhos");
        }
    }
}
