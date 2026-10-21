using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPersonaAsistida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonasAsistidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelacionConCuidador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoDiscapacidad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObraSocial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Localidad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CuidadorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonasAsistidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonasAsistidas_AspNetUsers_CuidadorId",
                        column: x => x.CuidadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonasAsistidas_CuidadorId",
                table: "PersonasAsistidas",
                column: "CuidadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonasAsistidas");
        }
    }
}
