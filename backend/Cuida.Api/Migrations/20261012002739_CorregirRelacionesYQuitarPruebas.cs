using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class CorregirRelacionesYQuitarPruebas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonasAsistidas_AspNetUsers_ApplicationUserId",
                table: "PersonasAsistidas");

            migrationBuilder.DropTable(
                name: "Pruebas");

            migrationBuilder.DropIndex(
                name: "IX_PersonasAsistidas_ApplicationUserId",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "PersonasAsistidas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "PersonasAsistidas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pruebas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pruebas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonasAsistidas_ApplicationUserId",
                table: "PersonasAsistidas",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonasAsistidas_AspNetUsers_ApplicationUserId",
                table: "PersonasAsistidas",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
