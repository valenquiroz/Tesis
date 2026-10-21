using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    CuidadorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_AspNetUsers_CuidadorId",
                        column: x => x.CuidadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documentos_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_CuidadorId",
                table: "Documentos",
                column: "CuidadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_PersonaAsistidaId",
                table: "Documentos",
                column: "PersonaAsistidaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documentos");
        }
    }
}
