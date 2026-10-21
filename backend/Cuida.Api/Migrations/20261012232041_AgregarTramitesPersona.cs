using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTramitesPersona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TramitesPersona",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TramitesPersona", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TramitesPersona_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TramitesPersona_PersonaAsistidaId_Codigo",
                table: "TramitesPersona",
                columns: new[] { "PersonaAsistidaId", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TramitesPersona");
        }
    }
}
