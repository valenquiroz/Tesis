using Cuida.Api.Data;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Controllers;

// Autoevaluación del cuidador: cada uno ve solo las suyas
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cuidador")]
public class BienestarController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BienestarController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();
        if (!(await _context.PlanDelUsuarioAsync(usuarioId)).Bienestar) return SinPlan();

        var historial = await _context.EvaluacionesEsfuerzo
            .AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId)
            .OrderByDescending(e => e.Fecha)
            .Take(12)
            .ToListAsync();

        var ultima = historial.FirstOrDefault();

        return Ok(new
        {
            EscalaEsfuerzo.Preguntas,
            EscalaEsfuerzo.PuntoDeCorte,
            EscalaEsfuerzo.Referencia,
            Ultimo = ultima == null ? null : Resultado(ultima),
            Historial = historial.Select(e => new
            {
                e.Id,
                Fecha = Formato.Utc(e.Fecha),
                e.Puntaje,
                Elevado = e.Puntaje >= EscalaEsfuerzo.PuntoDeCorte,
            }),
        });
    }

    private BadRequestObjectResult SinPlan() =>
        BadRequest(new { message = $"Tu bienestar está desde el plan {Planes.QueIncluye(l => l.Bienestar)}.", codigo = "plan" });

    [HttpPost]
    public async Task<IActionResult> Responder(ResponderEvaluacionDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();
        if (!(await _context.PlanDelUsuarioAsync(usuarioId)).Bienestar) return SinPlan();

        if (datos.Respuestas.Length != EscalaEsfuerzo.Preguntas.Length)
            return BadRequest(new { message = "Respondé todas las preguntas." });

        var evaluacion = new EvaluacionEsfuerzo
        {
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            Respuestas = string.Join(',', datos.Respuestas.Select(r => r ? 1 : 0)),
            Puntaje = datos.Respuestas.Count(r => r),
        };

        _context.EvaluacionesEsfuerzo.Add(evaluacion);
        await _context.SaveChangesAsync();

        return Ok(Resultado(evaluacion));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();
        if (!(await _context.PlanDelUsuarioAsync(usuarioId)).Bienestar) return SinPlan();

        var borradas = await _context.EvaluacionesEsfuerzo
            .Where(e => e.Id == id && e.UsuarioId == usuarioId)
            .ExecuteDeleteAsync();

        return borradas == 0
            ? NotFound(new { message = "No se encontró la evaluación." })
            : Ok(new { message = "Evaluación eliminada." });
    }

    private static object Resultado(EvaluacionEsfuerzo evaluacion)
    {
        var respuestas = evaluacion.Respuestas.Split(',').Select(r => r == "1").ToArray();
        var resultado = EscalaEsfuerzo.Evaluar(respuestas);

        return new
        {
            evaluacion.Id,
            Fecha = Formato.Utc(evaluacion.Fecha),
            resultado.Puntaje,
            resultado.Maximo,
            resultado.Elevado,
            resultado.Nivel,
            resultado.Mensaje,
            resultado.Sugerencias,
        };
    }
}
