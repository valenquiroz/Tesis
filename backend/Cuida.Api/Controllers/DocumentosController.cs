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
public class DocumentosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AlmacenamientoDocumentos _almacenamiento;

    public DocumentosController(
        ApplicationDbContext context,
        AlmacenamientoDocumentos almacenamiento)
    {
        _context = context;
        _almacenamiento = almacenamiento;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocumentos()
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasDe(cuidadorId);

        var documentos = await _context.Documentos
            .AsNoTracking()
            .Where(d => personasIds.Contains(d.PersonaAsistidaId))
            .OrderByDescending(d => d.FechaCarga)
            .Select(d => new
            {
                d.Id,
                d.NombreOriginal,
                d.TipoContenido,
                d.TamanoBytes,
                d.FechaCarga,
                d.PersonaAsistidaId,
                d.TramiteCodigo,
                PersonaAsistida = new { d.PersonaAsistida!.NombreCompleto }
            })
            .ToListAsync();

        // FechaCarga está en UTC, sin esto el front la muestra 3 hs corrida
        return Ok(documentos.Select(d => new
        {
            d.Id,
            d.NombreOriginal,
            d.TipoContenido,
            d.TamanoBytes,
            FechaCarga = DateTime.SpecifyKind(d.FechaCarga, DateTimeKind.Utc),
            d.PersonaAsistidaId,
            d.TramiteCodigo,
            d.PersonaAsistida
        }));
    }

    [HttpPost("subir")]
    [RequestSizeLimit(AlmacenamientoDocumentos.TamanoMaximoBytes + 1024 * 1024)]
    public async Task<IActionResult> SubirDocumento(
        [FromQuery] int personaAsistidaId,
        IFormFile? archivo,
        [FromQuery] string? tramiteCodigo = null)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasQueGestiona(cuidadorId);

        if (archivo == null)
        {
            return BadRequest(new
            {
                message = "No se recibió ningún archivo."
            });
        }

        var persona = await _context.PersonaQueGestionaAsync(cuidadorId, personaAsistidaId);

        if (persona == null)
        {
            return BadRequest(new
            {
                message = "La persona asistida no existe o no pertenece al cuidador."
            });
        }

        var limites = await _context.PlanDePersonaAsync(personaAsistidaId);

        if (limites.EspacioDocumentos is long espacio)
        {
            // el espacio es del responsable: suma lo de todas sus personas
            var usado = await _context.Documentos
                .Where(d => d.PersonaAsistida!.ResponsableId == persona.ResponsableId)
                .SumAsync(d => (long?)d.TamanoBytes) ?? 0;

            if (usado + archivo.Length > espacio)
            {
                return BadRequest(new
                {
                    message = $"Se llenó el espacio para documentos del plan {limites.Nombre} ({espacio / (1024 * 1024)} MB). Podés borrar documentos viejos o pasarte a un plan con más espacio.",
                    codigo = "plan"
                });
            }
        }

        if (tramiteCodigo != null && GuiaTramites.Pasos.All(p => p.Codigo != tramiteCodigo))
        {
            return BadRequest(new { message = "Ese trámite no existe en la guía." });
        }

        var error = await _almacenamiento.ValidarAsync(archivo);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        var (nombreArchivo, ruta) = await _almacenamiento.GuardarAsync(archivo);

        var documento = new Documento
        {
            NombreArchivo = nombreArchivo,
            NombreOriginal = Path.GetFileName(archivo.FileName),
            TipoContenido = AlmacenamientoDocumentos.TipoContenido(archivo.FileName),
            TamanoBytes = archivo.Length,
            RutaArchivo = ruta,
            FechaCarga = DateTime.UtcNow,
            PersonaAsistidaId = personaAsistidaId,
            TramiteCodigo = tramiteCodigo,
            CuidadorId = cuidadorId
        };

        _context.Documentos.Add(documento);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Documento cargado correctamente.",
            documento.Id,
            documento.NombreOriginal,
            documento.TamanoBytes,
            FechaCarga = DateTime.SpecifyKind(documento.FechaCarga, DateTimeKind.Utc),
            documento.PersonaAsistidaId,
            documento.TramiteCodigo
        });
    }

    [HttpGet("{id}/descargar")]
    public async Task<IActionResult> DescargarDocumento(int id)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasDe(cuidadorId);

        var documento = await _context.Documentos
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id == id &&
                personasIds.Contains(d.PersonaAsistidaId));

        if (documento == null)
        {
            return NotFound(new
            {
                message = "No se encontró el documento."
            });
        }

        if (!System.IO.File.Exists(documento.RutaArchivo))
        {
            return NotFound(new
            {
                message = "El archivo no se encuentra disponible."
            });
        }

        return PhysicalFile(
            documento.RutaArchivo,
            documento.TipoContenido ?? "application/octet-stream",
            documento.NombreOriginal);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarDocumento(int id)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
            return Unauthorized();

        var personasIds = _context.IdsPersonasQueGestiona(cuidadorId);

        var documento = await _context.Documentos
            .FirstOrDefaultAsync(d =>
                d.Id == id &&
                personasIds.Contains(d.PersonaAsistidaId));

        if (documento == null)
        {
            return NotFound(new
            {
                message = "No se encontró el documento."
            });
        }

        _context.Documentos.Remove(documento);

        await _context.SaveChangesAsync();

        _almacenamiento.Eliminar(documento.RutaArchivo);

        return Ok(new
        {
            message = "Documento eliminado correctamente."
        });
    }
}
