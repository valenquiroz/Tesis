using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarContactosEmergencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactosEmergencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoContacto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    CuidadorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactosEmergencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactosEmergencia_AspNetUsers_CuidadorId",
                        column: x => x.CuidadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactosEmergencia_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactosEmergencia_CuidadorId",
                table: "ContactosEmergencia",
                column: "CuidadorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactosEmergencia_PersonaAsistidaId",
                table: "ContactosEmergencia",
                column: "PersonaAsistidaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactosEmergencia");
        }
    }
}
