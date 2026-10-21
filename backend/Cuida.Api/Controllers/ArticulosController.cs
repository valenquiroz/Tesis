using Cuida.Api.Data;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Controllers;

// Artículos ya publicados por especialistas, con enlace a la fuente.
// Los lee cualquiera con cuenta; los carga y edita el administrador.
[ApiController]
[Route("api/[controller]")]
public class ArticulosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ArticulosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Cuidador,Administrador")]
    public async Task<IActionResult> Listar()
    {
        var usuarioId = User.UsuarioId();
        if (!User.IsInRole("Administrador") && (string.IsNullOrEmpty(usuarioId) || !(await _context.PlanDelUsuarioAsync(usuarioId)).Articulos))
            return BadRequest(new { message = $"Los artículos están en el plan {Planes.QueIncluye(l => l.Articulos)}.", codigo = "plan" });

        var articulos = await _context.ArticulosRecomendados
            .AsNoTracking()
            .Where(a => a.Activo)
            .OrderBy(a => a.TipoDiscapacidad == null ? 0 : 1)
            .ThenByDescending(a => a.Anio)
            .ThenBy(a => a.Titulo)
            .ToListAsync();

        return Ok(articulos);
    }

    // incluye los desactivados
    [HttpGet("todos")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ListarTodos() =>
        Ok(await _context.ArticulosRecomendados.AsNoTracking().OrderBy(a => a.Titulo).ToListAsync());

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear(ArticuloRecomendadoDto datos)
    {
        var articulo = new ArticuloRecomendado();
        CopiarDatos(datos, articulo);

        _context.ArticulosRecomendados.Add(articulo);
        await _context.SaveChangesAsync();

        return Ok(articulo);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Actualizar(int id, ArticuloRecomendadoDto datos)
    {
        var articulo = await _context.ArticulosRecomendados.FindAsync(id);
        if (articulo == null)
            return NotFound(new { message = "No se encontró el artículo." });

        CopiarDatos(datos, articulo);
        await _context.SaveChangesAsync();

        return Ok(articulo);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var articulo = await _context.ArticulosRecomendados.FindAsync(id);
        if (articulo == null)
            return NotFound(new { message = "No se encontró el artículo." });

        _context.ArticulosRecomendados.Remove(articulo);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Artículo eliminado." });
    }

    private static void CopiarDatos(ArticuloRecomendadoDto origen, ArticuloRecomendado destino)
    {
        destino.Titulo = origen.Titulo.Trim();
        destino.Autores = origen.Autores.Trim();
        destino.Fuente = origen.Fuente.Trim();
        destino.Anio = origen.Anio;
        destino.Resumen = origen.Resumen.Trim();
        destino.TipoDiscapacidad = string.IsNullOrWhiteSpace(origen.TipoDiscapacidad) ? null : origen.TipoDiscapacidad.Trim();
        destino.Enlace = origen.Enlace.Trim();
        destino.Activo = origen.Activo;
    }
}
