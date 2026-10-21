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
public class ContactosEmergenciaController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContactosEmergenciaController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetContactos()
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasDe(cuidadorId);

        var contactos = await _context.ContactosEmergencia
            .AsNoTracking()
            .Include(c => c.PersonaAsistida)
            .Where(c => personasIds.Contains(c.PersonaAsistidaId))
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return Ok(contactos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContacto(int id)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasDe(cuidadorId);

        var contacto = await _context.ContactosEmergencia
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                personasIds.Contains(c.PersonaAsistidaId));

        if (contacto == null)
        {
            return NotFound(new
            {
                message = "No se encontró el contacto de emergencia."
            });
        }

        return Ok(contacto);
    }

    [HttpPost]
public async Task<IActionResult> CrearContacto(
    CrearContactoEmergenciaDto datos)
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

    var contacto = new ContactoEmergencia
    {
        Nombre = datos.Nombre,
        Relacion = datos.Relacion,
        Telefono = datos.Telefono,
        TipoContacto = datos.TipoContacto,
        PersonaAsistidaId = datos.PersonaAsistidaId,
        CuidadorId = cuidadorId
    };

    _context.ContactosEmergencia.Add(contacto);

    await _context.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetContacto),
        new { id = contacto.Id },
        contacto);
}

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarContacto(
        int id,
        CrearContactoEmergenciaDto datos)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasQueGestiona(cuidadorId);

        var contacto = await _context.ContactosEmergencia
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                personasIds.Contains(c.PersonaAsistidaId));

        if (contacto == null)
        {
            return NotFound(new
            {
                message = "No se encontró el contacto de emergencia."
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

        contacto.Nombre = datos.Nombre;
        contacto.Relacion = datos.Relacion;
        contacto.Telefono = datos.Telefono;
        contacto.TipoContacto = datos.TipoContacto;
        contacto.PersonaAsistidaId = datos.PersonaAsistidaId;

        await _context.SaveChangesAsync();

        return Ok(contacto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarContacto(int id)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasQueGestiona(cuidadorId);

        var contacto = await _context.ContactosEmergencia
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                personasIds.Contains(c.PersonaAsistidaId));

        if (contacto == null)
        {
            return NotFound(new
            {
                message = "No se encontró el contacto de emergencia."
            });
        }

        _context.ContactosEmergencia.Remove(contacto);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Contacto de emergencia eliminado correctamente."
        });
    }
}