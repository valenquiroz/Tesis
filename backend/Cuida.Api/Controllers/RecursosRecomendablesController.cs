using Cuida.Api.Data;
using Cuida.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecursosRecomendablesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RecursosRecomendablesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Cuidador,Administrador")]
    public async Task<IActionResult> ObtenerTodos()
    {
        var recursos = await _context.RecursosRecomendables
            .AsNoTracking()
            .Where(r => r.Activo)
            .OrderBy(r => r.Categoria)
            .ThenBy(r => r.Titulo)
            .ToListAsync();

        return Ok(recursos);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Cuidador,Administrador")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var recurso = await _context.RecursosRecomendables
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recurso == null)
        {
            return NotFound(new
            {
                message = "No se encontró el recurso."
            });
        }

        return Ok(recurso);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear(RecursoRecomendable recurso)
    {
        recurso.Id = 0;

        _context.RecursosRecomendables.Add(recurso);

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = recurso.Id }, recurso);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Actualizar(int id, RecursoRecomendable recurso)
    {
        var existente = await _context.RecursosRecomendables
            .FirstOrDefaultAsync(r => r.Id == id);

        if (existente == null)
        {
            return NotFound(new
            {
                message = "No se encontró el recurso."
            });
        }

        existente.Titulo = recurso.Titulo;
        existente.Descripcion = recurso.Descripcion;
        existente.Categoria = recurso.Categoria;
        existente.TipoDiscapacidad = recurso.TipoDiscapacidad;
        existente.ObraSocial = recurso.ObraSocial;
        existente.Localidad = recurso.Localidad;
        existente.Necesidades = recurso.Necesidades;
        existente.Enlace = recurso.Enlace;
        existente.Activo = recurso.Activo;

        await _context.SaveChangesAsync();

        return Ok(existente);
    }

    // baja lógica
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var recurso = await _context.RecursosRecomendables
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recurso == null)
        {
            return NotFound(new
            {
                message = "No se encontró el recurso."
            });
        }

        recurso.Activo = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Recurso desactivado correctamente."
        });
    }
}
