using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTurnos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "PersonasAsistidas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Turnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lugar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Profesional = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Especialidad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    CuidadorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turnos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Turnos_AspNetUsers_CuidadorId",
                        column: x => x.CuidadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Turnos_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonasAsistidas_ApplicationUserId",
                table: "PersonasAsistidas",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_CuidadorId",
                table: "Turnos",
                column: "CuidadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_PersonaAsistidaId",
                table: "Turnos",
                column: "PersonaAsistidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonasAsistidas_AspNetUsers_ApplicationUserId",
                table: "PersonasAsistidas",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonasAsistidas_AspNetUsers_ApplicationUserId",
                table: "PersonasAsistidas");

            migrationBuilder.DropTable(
                name: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_PersonasAsistidas_ApplicationUserId",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "PersonasAsistidas");
        }
    }
}
