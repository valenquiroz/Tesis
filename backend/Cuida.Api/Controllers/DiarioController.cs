using Cuida.Api.Data;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cuidador")]
public class DiarioController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DiarioController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEntradas([FromQuery] int personaId, [FromQuery] int dias = 30)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        if (!await _context.PuedeVerAsync(usuarioId, personaId))
            return NotFound(new { message = "No se encontró la persona asistida." });

        if (!(await _context.PlanDePersonaAsync(personaId)).Diario)
            return SinPlan();

        var desde = DateTime.Today.AddDays(-Math.Clamp(dias, 1, 365));

        var entradas = await ConsultarAsync(personaId, desde, usuarioId);

        return Ok(new { Tipos = TiposDiario.Todos, Entradas = entradas });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(EntradaDiarioDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var error = await ValidarAsync(usuarioId, datos);
        if (error != null) return BadRequest(new { message = error });

        var entrada = new EntradaDiario { AutorId = usuarioId, FechaCreacion = DateTime.UtcNow };
        CopiarDatos(datos, entrada);

        _context.EntradasDiario.Add(entrada);
        await _context.SaveChangesAsync();

        return Ok(new { entrada.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, EntradaDiarioDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var entrada = await _context.EntradasDiario.FirstOrDefaultAsync(e => e.Id == id && e.AutorId == usuarioId);
        if (entrada == null)
            return NotFound(new { message = "Solo podés editar los registros que escribiste vos." });

        var error = await ValidarAsync(usuarioId, datos);
        if (error != null) return BadRequest(new { message = error });

        CopiarDatos(datos, entrada);
        await _context.SaveChangesAsync();

        return Ok(new { entrada.Id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var entrada = await _context.EntradasDiario.FirstOrDefaultAsync(e => e.Id == id && e.AutorId == usuarioId);
        if (entrada == null)
            return NotFound(new { message = "Solo podés borrar los registros que escribiste vos." });

        _context.EntradasDiario.Remove(entrada);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registro eliminado." });
    }

    // Todo junto para imprimir y llevar al médico o a la junta
    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen([FromQuery] int personaId, [FromQuery] int dias = 30)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var persona = await _context.PersonaDeAsync(usuarioId, personaId);
        if (persona == null)
            return NotFound(new { message = "No se encontró la persona asistida." });

        var limites = await _context.PlanDePersonaAsync(personaId);
        if (!limites.Diario)
            return SinPlan();

        // el plan Equipo imprime el último mes; Plus, el historial de hasta un año
        dias = Math.Clamp(dias, 7, limites.Informe ? 365 : 30);
        var desde = DateTime.Today.AddDays(-dias);

        var entradas = await ConsultarAsync(personaId, desde, usuarioId);

        var medicamentos = await _context.Medicamentos
            .AsNoTracking()
            .Where(m => m.PersonaAsistidaId == personaId && m.Activo)
            .OrderBy(m => m.Nombre)
            .Select(m => new
            {
                m.Nombre,
                m.Dosis,
                m.Horarios,
                m.Indicaciones,
                m.Medico,
                Tomadas = m.Tomas.Count(t => t.Fecha >= desde && t.Estado == EstadosToma.Tomada),
                Omitidas = m.Tomas.Count(t => t.Fecha >= desde && t.Estado == EstadosToma.Omitida),
            })
            .ToListAsync();

        // las que se registraron como "no se tomó", para que el médico vea cuáles y cuándo
        var noDadas = await _context.TomasMedicamentos
            .AsNoTracking()
            .Where(t => t.Medicamento!.PersonaAsistidaId == personaId && t.Fecha >= desde && t.Estado == EstadosToma.Omitida)
            .OrderByDescending(t => t.Fecha)
            .ThenByDescending(t => t.Horario)
            .Take(30)
            .Select(t => new { Fecha = t.Fecha.ToString("yyyy-MM-dd"), t.Horario, Medicamento = t.Medicamento!.Nombre, RegistradoPor = t.RegistradoPor!.NombreCompleto })
            .ToListAsync();

        var tomas = await _context.TomasMedicamentos
            .AsNoTracking()
            .Where(t => t.Medicamento!.PersonaAsistidaId == personaId && t.Fecha >= desde)
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var turnos = await _context.Turnos
            .AsNoTracking()
            .Where(t => t.PersonaAsistidaId == personaId && t.FechaHora >= desde)
            .OrderBy(t => t.FechaHora)
            .Select(t => new { t.Titulo, t.FechaHora, t.Profesional, t.Especialidad })
            .ToListAsync();

        return Ok(new
        {
            Persona = new
            {
                persona.NombreCompleto,
                persona.FechaNacimiento,
                persona.TipoDiscapacidad,
                persona.Diagnostico,
                persona.ObraSocial,
                persona.NumeroAfiliado,
                persona.Alergias,
            },
            Desde = desde.ToString("yyyy-MM-dd"),
            Hasta = DateTime.Today.ToString("yyyy-MM-dd"),
            Dias = dias,
            PorTipo = entradas
                .GroupBy(e => e.Tipo)
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .OrderByDescending(g => g.Cantidad),
            Entradas = entradas,
            Medicamentos = medicamentos,
            Tomas = new
            {
                Tomadas = tomas.FirstOrDefault(t => t.Estado == EstadosToma.Tomada)?.Cantidad ?? 0,
                Omitidas = tomas.FirstOrDefault(t => t.Estado == EstadosToma.Omitida)?.Cantidad ?? 0,
            },
            NoDadas = noDadas,
            Turnos = turnos,
        });
    }

    private async Task<List<EntradaVista>> ConsultarAsync(int personaId, DateTime desde, string usuarioId)
    {
        var entradas = await _context.EntradasDiario
            .AsNoTracking()
            .Where(e => e.PersonaAsistidaId == personaId && e.Fecha >= desde)
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.FechaCreacion)
            .Select(e => new
            {
                e.Id,
                e.Fecha,
                e.Tipo,
                e.Intensidad,
                e.Texto,
                e.AutorId,
                Autor = e.Autor!.NombreCompleto,
            })
            .ToListAsync();

        return entradas
            .Select(e => new EntradaVista(e.Id, e.Fecha, e.Tipo, e.Intensidad, e.Texto, e.Autor, e.AutorId == usuarioId))
            .ToList();
    }

    private async Task<string?> ValidarAsync(string usuarioId, EntradaDiarioDto datos)
    {
        if (!TiposDiario.Todos.Contains(datos.Tipo))
            return "El tipo de registro no es válido.";

        if (datos.Fecha.Date > DateTime.Today)
            return "La fecha no puede ser futura.";

        if (!await _context.PuedeVerAsync(usuarioId, datos.PersonaAsistidaId))
            return "La persona asistida no existe o no la acompañás.";

        if (!await _context.PuedeRegistrarAsync(usuarioId, datos.PersonaAsistidaId))
            return "Estás en modo observador: el diario lo escriben los cuidadores.";

        if (!(await _context.PlanDePersonaAsync(datos.PersonaAsistidaId)).Diario)
            return $"El diario está desde el plan {Planes.QueIncluye(l => l.Diario)}.";

        return null;
    }

    private BadRequestObjectResult SinPlan() =>
        BadRequest(new { message = $"El diario de cuidado está desde el plan {Planes.QueIncluye(l => l.Diario)}.", codigo = "plan" });

    private static void CopiarDatos(EntradaDiarioDto origen, EntradaDiario destino)
    {
        destino.PersonaAsistidaId = origen.PersonaAsistidaId;
        destino.Fecha = origen.Fecha;
        destino.Tipo = origen.Tipo;
        destino.Intensidad = origen.Intensidad;
        destino.Texto = origen.Texto.Trim();
    }

    public record EntradaVista(int Id, DateTime Fecha, string Tipo, int? Intensidad, string Texto, string Autor, bool EsMia);
}
