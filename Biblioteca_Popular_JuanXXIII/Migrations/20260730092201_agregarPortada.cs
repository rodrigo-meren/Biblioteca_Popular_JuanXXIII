using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca_Popular_JuanXXIII.Migrations
{
    /// <inheritdoc />
    public partial class agregarPortada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortadaUrl",
                table: "Libros",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortadaUrl",
                table: "Libros");
        }
    }
}
