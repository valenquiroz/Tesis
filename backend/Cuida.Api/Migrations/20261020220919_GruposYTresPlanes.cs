using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class GruposYTresPlanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreGrupo",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            // las instituciones pasan a ser el grupo de su gerente, con el plan Plus
            migrationBuilder.Sql(@"
                UPDATE u SET NombreGrupo = o.Nombre, [Plan] = 'Plus'
                FROM AspNetUsers u JOIN Organizaciones o ON o.Id = u.OrganizacionId
                WHERE u.RolOrganizacion = 'Gerente';
                UPDATE AspNetUsers SET [Plan] = 'Equipo' WHERE [Plan] = 'Familia';
                UPDATE AspNetUsers SET [Plan] = 'Plus' WHERE [Plan] = 'Institucion';");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizaciones_OrganizacionId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonasAsistidas_Organizaciones_OrganizacionId",
                table: "PersonasAsistidas");

            migrationBuilder.DropTable(
                name: "Organizaciones");

            migrationBuilder.DropIndex(
                name: "IX_PersonasAsistidas_OrganizacionId",
                table: "PersonasAsistidas");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizacionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RolOrganizacion",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreGrupo",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "PersonasAsistidas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolOrganizacion",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organizaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LimitePacientes = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizaciones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonasAsistidas_OrganizacionId",
                table: "PersonasAsistidas",
                column: "OrganizacionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizacionId",
                table: "AspNetUsers",
                column: "OrganizacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizaciones_OrganizacionId",
                table: "AspNetUsers",
                column: "OrganizacionId",
                principalTable: "Organizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonasAsistidas_Organizaciones_OrganizacionId",
                table: "PersonasAsistidas",
                column: "OrganizacionId",
                principalTable: "Organizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
