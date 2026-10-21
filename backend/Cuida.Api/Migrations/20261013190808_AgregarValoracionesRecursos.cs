using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarValoracionesRecursos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValoracionesRecursos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    RecursoRecomendableId = table.Column<int>(type: "int", nullable: false),
                    Util = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoracionesRecursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoracionesRecursos_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValoracionesRecursos_RecursosRecomendables_RecursoRecomendableId",
                        column: x => x.RecursoRecomendableId,
                        principalTable: "RecursosRecomendables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValoracionesRecursos_PersonaAsistidaId_RecursoRecomendableId",
                table: "ValoracionesRecursos",
                columns: new[] { "PersonaAsistidaId", "RecursoRecomendableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoracionesRecursos_RecursoRecomendableId",
                table: "ValoracionesRecursos",
                column: "RecursoRecomendableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValoracionesRecursos");
        }
    }
}
