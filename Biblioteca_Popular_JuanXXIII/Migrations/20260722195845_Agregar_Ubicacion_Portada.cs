using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca_Popular_JuanXXIII.Migrations
{
    /// <inheritdoc />
    public partial class Agregar_Ubicacion_Portada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipoMaterial",
                table: "Libros",
                newName: "UrlPortada");

            migrationBuilder.RenameColumn(
                name: "SignaturaDewey",
                table: "Libros",
                newName: "UbicacionFisica");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UrlPortada",
                table: "Libros",
                newName: "TipoMaterial");

            migrationBuilder.RenameColumn(
                name: "UbicacionFisica",
                table: "Libros",
                newName: "SignaturaDewey");
        }
    }
}
