using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class ObservadoresInstitucionesYPlanes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "PublicacionesUtiles");

            migrationBuilder.DropTable(
                name: "ReportesComunidad");

            migrationBuilder.DropTable(
                name: "RespuestasPublicaciones");

            migrationBuilder.DropTable(
                name: "Publicaciones");

            migrationBuilder.DropColumn(
                name: "Profesion",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "PersonasCompartidas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Cuidador");

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "PersonasAsistidas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ResponsableCuida",
                table: "PersonasAsistidas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacionId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Gratis");

            migrationBuilder.AddColumn<string>(
                name: "RolOrganizacion",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArticulosRecomendados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Autores = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Fuente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: true),
                    Resumen = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    TipoDiscapacidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Enlace = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticulosRecomendados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LimitePacientes = table.Column<int>(type: "int", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizaciones_OrganizacionId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonasAsistidas_Organizaciones_OrganizacionId",
                table: "PersonasAsistidas");

            migrationBuilder.DropTable(
                name: "ArticulosRecomendados");

            migrationBuilder.DropTable(
                name: "Organizaciones");

            migrationBuilder.DropIndex(
                name: "IX_PersonasAsistidas_OrganizacionId",
                table: "PersonasAsistidas");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizacionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "PersonasCompartidas");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "ResponsableCuida",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "OrganizacionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RolOrganizacion",
                table: "AspNetUsers");

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
                    AutorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resumen = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
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
                    AutorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
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
                    AutorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PublicacionId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ReportesComunidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicacionId = table.Column<int>(type: "int", nullable: true),
                    RespuestaId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Resuelto = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportesComunidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportesComunidad_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportesComunidad_Publicaciones_PublicacionId",
                        column: x => x.PublicacionId,
                        principalTable: "Publicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportesComunidad_RespuestasPublicaciones_RespuestaId",
                        column: x => x.RespuestaId,
                        principalTable: "RespuestasPublicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_ReportesComunidad_PublicacionId",
                table: "ReportesComunidad",
                column: "PublicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesComunidad_RespuestaId",
                table: "ReportesComunidad",
                column: "RespuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesComunidad_UsuarioId",
                table: "ReportesComunidad",
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
    }
}
