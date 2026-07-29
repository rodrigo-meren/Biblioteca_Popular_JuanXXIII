using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biblioteca_Popular_JuanXXIII.Migrations
{
    /// <inheritdoc />
    public partial class VincularSocioIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId",
                table: "Socios",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Socios");
        }
    }
}
