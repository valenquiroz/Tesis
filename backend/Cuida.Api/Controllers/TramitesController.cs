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
public class TramitesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TramitesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Hoja de ruta de la persona: los pasos que le corresponden y cómo va cada uno
    [HttpGet("{personaId:int}")]
    public async Task<IActionResult> GetHojaDeRuta(int personaId)
    {
        var persona = await BuscarPersonaAsync(personaId);
        if (persona == null)
            return NotFound(new { message = "No se encontró la persona asistida." });

        if (!(await _context.PlanDePersonaAsync(personaId)).Tramites)
            return SinPlan();

        var avances = await _context.TramitesPersona
            .AsNoTracking()
            .Where(t => t.PersonaAsistidaId == personaId)
            .ToDictionaryAsync(t => t.Codigo);

        var adjuntos = await _context.Documentos
            .AsNoTracking()
            .Where(d => d.PersonaAsistidaId == personaId && d.TramiteCodigo != null)
            .OrderBy(d => d.FechaCarga)
            .Select(d => new { d.Id, d.NombreOriginal, d.TramiteCodigo })
            .ToListAsync();

        var pasos = GuiaTramites.ParaPersona(persona, DateTime.Today)
            .Select(p =>
            {
                avances.TryGetValue(p.Codigo, out var avance);
                return new
                {
                    p.Codigo,
                    p.Etapa,
                    p.Titulo,
                    p.Resumen,
                    p.Pasos,
                    Documentacion = p.Documentacion.Select(d => d.Texto),
                    p.Aviso,
                    p.Organismo,
                    p.Enlace,
                    p.Fuente,
                    p.Imprescindible,
                    Documentos = adjuntos
                        .Where(d => d.TramiteCodigo == p.Codigo)
                        .Select(d => new { d.Id, d.NombreOriginal }),
                    Estado = avance?.Estado ?? EstadosTramite.Pendiente,
                    avance?.Nota,
                    FechaActualizacion = Formato.Utc(avance?.FechaActualizacion),
                };
            })
            .ToList();

        return Ok(new
        {
            Etapas = GuiaTramites.Etapas,
            Pasos = pasos,
            TipoDiscapacidad = persona.TipoDiscapacidad,
            Revision = GuiaTramites.FechaRevision,
        });
    }

    [HttpPut("{personaId:int}/{codigo}")]
    public async Task<IActionResult> ActualizarEstado(int personaId, string codigo, ActualizarTramiteDto datos)
    {
        if (!EstadosTramite.Todos.Contains(datos.Estado))
            return BadRequest(new { message = "El estado no es válido." });

        if (GuiaTramites.Pasos.All(p => p.Codigo != codigo))
            return NotFound(new { message = "Ese trámite no existe en la guía." });

        var persona = await BuscarPersonaAsync(personaId, paraGestionar: true);
        if (persona == null)
            return NotFound(new { message = "No se encontró la persona asistida." });

        if (!(await _context.PlanDePersonaAsync(personaId)).Tramites)
            return SinPlan();

        var avance = await _context.TramitesPersona
            .FirstOrDefaultAsync(t => t.PersonaAsistidaId == personaId && t.Codigo == codigo);

        if (avance == null)
        {
            avance = new TramitePersona { PersonaAsistidaId = personaId, Codigo = codigo };
            _context.TramitesPersona.Add(avance);
        }

        avance.Estado = datos.Estado;
        avance.Nota = string.IsNullOrWhiteSpace(datos.Nota) ? null : datos.Nota.Trim();
        avance.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            avance.Codigo,
            avance.Estado,
            avance.Nota,
            FechaActualizacion = Formato.Utc(avance.FechaActualizacion),
        });
    }

    private BadRequestObjectResult SinPlan() =>
        BadRequest(new { message = $"La guía de trámites está desde el plan {Planes.QueIncluye(l => l.Tramites)}.", codigo = "plan" });

    private async Task<PersonaAsistida?> BuscarPersonaAsync(int personaId, bool paraGestionar = false)
    {
        var cuidadorId = User.UsuarioId();
        if (string.IsNullOrEmpty(cuidadorId)) return null;

        var personas = paraGestionar ? _context.PersonasQueGestiona(cuidadorId) : _context.PersonasDe(cuidadorId);
        return await personas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == personaId);
    }
}
