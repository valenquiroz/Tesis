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
public class RecomendacionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RecomendacionService _recomendacionService;

    public RecomendacionesController(
        ApplicationDbContext context,
        RecomendacionService recomendacionService)
    {
        _context = context;
        _recomendacionService = recomendacionService;
    }

    [HttpGet("{personaAsistidaId}")]
    public async Task<IActionResult> Obtener(
        int personaAsistidaId)
    {
        var cuidadorId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al cuidador."
            });
        }

        var persona = await _context.PersonaDeAsync(cuidadorId, personaAsistidaId);

        if (persona == null)
        {
            return NotFound(new
            {
                message =
                    "La persona asistida no existe o no pertenece al cuidador."
            });
        }

        if (!(await _context.PlanDePersonaAsync(personaAsistidaId)).Recursos)
            return SinPlan();

        var recomendaciones =
            await _recomendacionService
                .ObtenerRecomendaciones(personaAsistidaId);

        return Ok(recomendaciones);
    }

    // "Me sirvió" (true), "No me sirvió" (false) o deshacer (null)
    [HttpPut("{personaAsistidaId}/valoraciones/{recursoId}")]
    public async Task<IActionResult> Valorar(
        int personaAsistidaId,
        int recursoId,
        ValorarRecursoDto dto)
    {
        var cuidadorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(cuidadorId))
        {
            return Unauthorized(new { message = "No se pudo identificar al cuidador." });
        }

        var persona = await _context.PersonaQueGestionaAsync(cuidadorId, personaAsistidaId);

        if (persona == null)
        {
            return NotFound(new { message = "La persona asistida no existe o no pertenece al cuidador." });
        }

        if (!(await _context.PlanDePersonaAsync(personaAsistidaId)).Recursos)
            return SinPlan();

        if (!await _context.RecursosRecomendables.AnyAsync(r => r.Id == recursoId && r.Activo))
        {
            return NotFound(new { message = "El recurso no existe." });
        }

        var valoracion = await _context.ValoracionesRecursos
            .FirstOrDefaultAsync(v => v.PersonaAsistidaId == personaAsistidaId && v.RecursoRecomendableId == recursoId);

        if (dto.Util == null)
        {
            if (valoracion != null)
                _context.ValoracionesRecursos.Remove(valoracion);
        }
        else if (valoracion == null)
        {
            _context.ValoracionesRecursos.Add(new ValoracionRecurso
            {
                PersonaAsistidaId = personaAsistidaId,
                RecursoRecomendableId = recursoId,
                Util = dto.Util.Value,
                Fecha = DateTime.UtcNow
            });
        }
        else
        {
            valoracion.Util = dto.Util.Value;
            valoracion.Fecha = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private BadRequestObjectResult SinPlan() =>
        BadRequest(new { message = $"Los recursos recomendados están desde el plan {Planes.QueIncluye(l => l.Recursos)}.", codigo = "plan" });
}
