using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarComunidadYArticulos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Profesion",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Resumen = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articulos_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Publicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Texto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AutorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Publicaciones_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicacionesUtiles",
                columns: table => new
                {
                    PublicacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacionesUtiles", x => new { x.PublicacionId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_PublicacionesUtiles_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicacionesUtiles_Publicaciones_PublicacionId",
                        column: x => x.PublicacionId,
                        principalTable: "Publicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasPublicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicacionId = table.Column<int>(type: "int", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AutorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasPublicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespuestasPublicaciones_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespuestasPublicaciones_Publicaciones_PublicacionId",
                        column: x => x.PublicacionId,
                        principalTable: "Publicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_AutorId",
                table: "Articulos",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_FechaPublicacion",
                table: "Articulos",
                column: "FechaPublicacion");

            migrationBuilder.CreateIndex(
                name: "IX_Publicaciones_AutorId",
                table: "Publicaciones",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Publicaciones_FechaCreacion",
                table: "Publicaciones",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_PublicacionesUtiles_UsuarioId",
                table: "PublicacionesUtiles",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasPublicaciones_AutorId",
                table: "RespuestasPublicaciones",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasPublicaciones_PublicacionId",
                table: "RespuestasPublicaciones",
                column: "PublicacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "PublicacionesUtiles");

            migrationBuilder.DropTable(
                name: "RespuestasPublicaciones");

            migrationBuilder.DropTable(
                name: "Publicaciones");

            migrationBuilder.DropColumn(
                name: "Profesion",
                table: "AspNetUsers");
        }
    }
}
