using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cuida.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMedicacionDiarioBienestarYCompartir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "Turnos",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Alergias",
                table: "PersonasAsistidas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diagnostico",
                table: "PersonasAsistidas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrupoSanguineo",
                table: "PersonasAsistidas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InformacionEmergencia",
                table: "PersonasAsistidas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoCabecera",
                table: "PersonasAsistidas",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroAfiliado",
                table: "PersonasAsistidas",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoMedico",
                table: "PersonasAsistidas",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VencimientoCud",
                table: "PersonasAsistidas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TramiteCodigo",
                table: "Documentos",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EntradasDiario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    AutorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Intensidad = table.Column<int>(type: "int", nullable: true),
                    Texto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradasDiario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntradasDiario_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntradasDiario_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluacionesEsfuerzo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Respuestas = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Puntaje = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionesEsfuerzo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluacionesEsfuerzo_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Medicamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Dosis = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Horarios = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Indicaciones = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Medico = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    VencimientoReceta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medicamentos_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonasCompartidas",
                columns: table => new
                {
                    PersonaAsistidaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonasCompartidas", x => new { x.PersonaAsistidaId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_PersonasCompartidas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonasCompartidas_PersonasAsistidas_PersonaAsistidaId",
                        column: x => x.PersonaAsistidaId,
                        principalTable: "PersonasAsistidas",
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
                    Motivo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "TomasMedicamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicamentoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Horario = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegistradoPorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TomasMedicamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TomasMedicamentos_AspNetUsers_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TomasMedicamentos_Medicamentos_MedicamentoId",
                        column: x => x.MedicamentoId,
                        principalTable: "Medicamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntradasDiario_AutorId",
                table: "EntradasDiario",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_EntradasDiario_PersonaAsistidaId_Fecha",
                table: "EntradasDiario",
                columns: new[] { "PersonaAsistidaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesEsfuerzo_UsuarioId",
                table: "EvaluacionesEsfuerzo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_PersonaAsistidaId",
                table: "Medicamentos",
                column: "PersonaAsistidaId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonasCompartidas_UsuarioId",
                table: "PersonasCompartidas",
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
                name: "IX_TomasMedicamentos_MedicamentoId_Fecha_Horario",
                table: "TomasMedicamentos",
                columns: new[] { "MedicamentoId", "Fecha", "Horario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TomasMedicamentos_RegistradoPorId",
                table: "TomasMedicamentos",
                column: "RegistradoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntradasDiario");

            migrationBuilder.DropTable(
                name: "EvaluacionesEsfuerzo");

            migrationBuilder.DropTable(
                name: "PersonasCompartidas");

            migrationBuilder.DropTable(
                name: "ReportesComunidad");

            migrationBuilder.DropTable(
                name: "TomasMedicamentos");

            migrationBuilder.DropTable(
                name: "Medicamentos");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "Alergias",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "Diagnostico",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "GrupoSanguineo",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "InformacionEmergencia",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "MedicoCabecera",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "NumeroAfiliado",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "TelefonoMedico",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "VencimientoCud",
                table: "PersonasAsistidas");

            migrationBuilder.DropColumn(
                name: "TramiteCodigo",
                table: "Documentos");
        }
    }
}
