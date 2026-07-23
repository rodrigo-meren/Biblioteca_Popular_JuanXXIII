using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca_Popular_JuanXXIII.Migrations
{
    /// <inheritdoc />
    public partial class AgregoResena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Resena",
                table: "Prestamos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resena",
                table: "Prestamos");
        }
    }
}
