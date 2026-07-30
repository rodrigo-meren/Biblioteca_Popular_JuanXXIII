using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca_Popular_JuanXXIII.Migrations
{
    /// <inheritdoc />
    public partial class DestacarLibro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsDestacado",
                table: "Libros",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsDestacado",
                table: "Libros");
        }
    }
}
