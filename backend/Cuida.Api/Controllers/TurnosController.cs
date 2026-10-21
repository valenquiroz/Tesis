using System.Security.Claims;
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
public class TurnosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TurnosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTurnos()
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasDe(cuidadorId);

        var turnos = await _context.Turnos
            .Include(t => t.PersonaAsistida)
            .Where(t => personasIds.Contains(t.PersonaAsistidaId))
            .OrderBy(t => t.FechaHora)
            .ToListAsync();

        return Ok(turnos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTurno(int id)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasDe(cuidadorId);

        var turno = await _context.Turnos
            .Include(t => t.PersonaAsistida)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                personasIds.Contains(t.PersonaAsistidaId));

        if (turno == null)
        {
            return NotFound(new
            {
                message = "No se encontró el turno."
            });
        }

        return Ok(turno);
    }

   [HttpPost]
public async Task<IActionResult> CrearTurno(CrearTurnoDto datos)
{
    var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(cuidadorId))
        return Unauthorized();

    var personasIds = _context.IdsPersonasQueGestiona(cuidadorId);

    var persona = await _context.PersonaQueGestionaAsync(cuidadorId, datos.PersonaAsistidaId);

    if (persona == null)
    {
        return BadRequest(new
        {
            message = "La persona asistida no existe o no pertenece al cuidador."
        });
    }

    var turno = new Turno
    {
        Titulo = datos.Titulo,
        FechaHora = datos.FechaHora,
        Lugar = datos.Lugar,
        Profesional = datos.Profesional,
        Especialidad = datos.Especialidad,
        Observaciones = datos.Observaciones,
        RepetirCadaMeses = datos.RepetirCadaMeses,
        PersonaAsistidaId = datos.PersonaAsistidaId,
        CuidadorId = cuidadorId
    };

    _context.Turnos.Add(turno);

    await _context.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetTurno),
        new { id = turno.Id },
        turno);
}

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarTurno(
        int id,
        CrearTurnoDto datos)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasQueGestiona(cuidadorId);

        var turno = await _context.Turnos
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                personasIds.Contains(t.PersonaAsistidaId));

        if (turno == null)
        {
            return NotFound(new
            {
                message = "No se encontró el turno."
            });
        }

        var persona = await _context.PersonaQueGestionaAsync(cuidadorId, datos.PersonaAsistidaId);

        if (persona == null)
        {
            return BadRequest(new
            {
                message = "La persona asistida no existe o no pertenece al cuidador."
            });
        }

        turno.Titulo = datos.Titulo;
        turno.FechaHora = datos.FechaHora;
        turno.Lugar = datos.Lugar;
        turno.Profesional = datos.Profesional;
        turno.Especialidad = datos.Especialidad;
        turno.Observaciones = datos.Observaciones;
        turno.RepetirCadaMeses = datos.RepetirCadaMeses;
        turno.PersonaAsistidaId = datos.PersonaAsistidaId;

        await _context.SaveChangesAsync();

        return Ok(turno);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarTurno(int id)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasQueGestiona(cuidadorId);

        var turno = await _context.Turnos
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                personasIds.Contains(t.PersonaAsistidaId));

        if (turno == null)
        {
            return NotFound(new
            {
                message = "No se encontró el turno."
            });
        }

        _context.Turnos.Remove(turno);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Turno eliminado correctamente."
        });
    }
}