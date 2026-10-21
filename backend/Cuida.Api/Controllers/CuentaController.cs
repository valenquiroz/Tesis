using Cuida.Api.Data;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Controllers;

// Plan y nombre del grupo. El pago está simulado: el prototipo cambia el plan sin cobrar.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cuidador")]
public class CuentaController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CuentaController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("plan")]
    public async Task<IActionResult> GetPlan()
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var yo = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == usuarioId);

        // si cuida en el grupo de otro, ese plan lo paga el dueño y también lo cubre a él
        var dondeAyuda = await _context.GruposDondeAyudaAsync(usuarioId);

        return Ok(new
        {
            Plan = Planes.De(yo.Plan).Codigo,
            yo.NombreGrupo,
            Uso = await UsoAsync(usuarioId),
            Planes = Planes.Limites.Values,
            DondeAyuda = dondeAyuda.Select(g => new { g.Nombre, g.Dueno, g.Plan, Personas = g.Personas.Count }),
        });
    }

    [HttpPut("plan")]
    public async Task<IActionResult> CambiarPlan(CambiarPlanDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        if (!Planes.Todos.Contains(datos.Plan))
            return BadRequest(new { message = "Ese plan no existe." });

        var yo = await _context.Users.FirstAsync(u => u.Id == usuarioId);

        var nuevo = Planes.De(datos.Plan);
        var uso = await UsoAsync(usuarioId);

        // no se puede bajar a un plan que no alcanza para lo que ya está cargado
        var problemas = new List<string>();
        if (nuevo.Personas is int personas && uso.Personas > personas)
            problemas.Add($"tenés {uso.Personas} personas a cargo y el plan {nuevo.Nombre} incluye {personas}");
        if (nuevo.Integrantes is int integrantes && uso.Integrantes > integrantes)
            problemas.Add(integrantes == 0
                ? "hay otras personas en tu grupo"
                : $"hay {uso.Integrantes} personas en tu grupo y el plan {nuevo.Nombre} permite {integrantes}");
        if (!nuevo.Grupo && uso.EnModoObservador > 0)
            problemas.Add("tenés personas en modo observador");
        if (nuevo.EspacioDocumentos is long espacio && uso.DocumentosBytes > espacio)
            problemas.Add($"tus documentos ocupan más de lo que incluye el plan {nuevo.Nombre}");

        if (problemas.Count > 0)
        {
            return BadRequest(new
            {
                message = $"No se puede pasar al plan {nuevo.Nombre}: {string.Join("; ", problemas)}."
            });
        }

        yo.Plan = nuevo.Codigo;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Listo, ahora tenés el plan {nuevo.Nombre}.", plan = nuevo.Codigo });
    }

    // cómo ven el grupo los cuidadores, por ejemplo "Residencia Las Glicinas"
    [HttpPut("grupo")]
    public async Task<IActionResult> CambiarNombreGrupo(NombreGrupoDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var yo = await _context.Users.FirstAsync(u => u.Id == usuarioId);
        yo.NombreGrupo = string.IsNullOrWhiteSpace(datos.NombreGrupo) ? null : datos.NombreGrupo.Trim();
        await _context.SaveChangesAsync();

        return Ok(new { message = "Se guardó el nombre del grupo.", nombreGrupo = yo.NombreGrupo });
    }

    private async Task<UsoPlan> UsoAsync(string usuarioId)
    {
        var propias = _context.PersonasAsistidas.Where(p => p.ResponsableId == usuarioId);

        return new UsoPlan(
            await propias.CountAsync(),
            await _context.PersonasCompartidas
                .Where(c => propias.Select(p => p.Id).Contains(c.PersonaAsistidaId))
                .Select(c => c.UsuarioId)
                .Distinct()
                .CountAsync(),
            await propias.CountAsync(p => !p.ResponsableCuida),
            await _context.Documentos
                .Where(d => propias.Select(p => p.Id).Contains(d.PersonaAsistidaId))
                .SumAsync(d => (long?)d.TamanoBytes) ?? 0);
    }

    private record UsoPlan(int Personas, int Integrantes, int EnModoObservador, long DocumentosBytes);
}
