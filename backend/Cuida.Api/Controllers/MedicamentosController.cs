using System.Globalization;
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
public class MedicamentosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MedicamentosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMedicamentos()
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var personasIds = _context.IdsPersonasDe(usuarioId);

        var medicamentos = await _context.Medicamentos
            .AsNoTracking()
            .Where(m => personasIds.Contains(m.PersonaAsistidaId))
            .OrderByDescending(m => m.Activo)
            .ThenBy(m => m.Nombre)
            .Select(m => new
            {
                m.Id,
                m.PersonaAsistidaId,
                PersonaAsistida = m.PersonaAsistida!.NombreCompleto,
                m.Nombre,
                m.Dosis,
                m.Horarios,
                m.Indicaciones,
                m.Medico,
                m.VencimientoReceta,
                m.Activo,
            })
            .ToListAsync();

        return Ok(medicamentos);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(MedicamentoDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        if (!await _context.PuedeGestionarAsync(usuarioId, datos.PersonaAsistidaId))
            return BadRequest(new { message = "La persona asistida no existe o no la acompañás." });

        var horarios = NormalizarHorarios(datos.Horarios);
        if (horarios == null)
            return BadRequest(new { message = "Revisá los horarios: usá el formato 08:00 y separalos con coma." });

        var medicamento = new Medicamento { PersonaAsistidaId = datos.PersonaAsistidaId };
        CopiarDatos(datos, medicamento, horarios);

        _context.Medicamentos.Add(medicamento);
        await _context.SaveChangesAsync();

        return Ok(medicamento);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, MedicamentoDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var medicamento = await BuscarAsync(usuarioId, id);
        if (medicamento == null)
            return NotFound(new { message = "No se encontró el medicamento." });

        if (!await _context.PuedeGestionarAsync(usuarioId, datos.PersonaAsistidaId))
            return BadRequest(new { message = "La persona asistida no existe o no la acompañás." });

        var horarios = NormalizarHorarios(datos.Horarios);
        if (horarios == null)
            return BadRequest(new { message = "Revisá los horarios: usá el formato 08:00 y separalos con coma." });

        medicamento.PersonaAsistidaId = datos.PersonaAsistidaId;
        CopiarDatos(datos, medicamento, horarios);

        await _context.SaveChangesAsync();

        return Ok(medicamento);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var medicamento = await BuscarAsync(usuarioId, id);
        if (medicamento == null)
            return NotFound(new { message = "No se encontró el medicamento." });

        _context.Medicamentos.Remove(medicamento);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Medicamento eliminado." });
    }

    // Todas las tomas de un día, de todas las personas que acompaña el usuario
    [HttpGet("agenda")]
    public async Task<IActionResult> GetAgenda([FromQuery] DateTime? fecha)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var dia = (fecha ?? DateTime.Today).Date;
        var personasIds = _context.IdsPersonasDe(usuarioId);

        var medicamentos = await _context.Medicamentos
            .AsNoTracking()
            .Where(m => m.Activo && personasIds.Contains(m.PersonaAsistidaId))
            .Select(m => new
            {
                m.Id,
                m.PersonaAsistidaId,
                PersonaAsistida = m.PersonaAsistida!.NombreCompleto,
                m.Nombre,
                m.Dosis,
                m.Indicaciones,
                m.Horarios,
            })
            .ToListAsync();

        var ids = medicamentos.Select(m => m.Id).ToList();
        var tomas = await _context.TomasMedicamentos
            .AsNoTracking()
            .Where(t => ids.Contains(t.MedicamentoId) && t.Fecha == dia)
            .Select(t => new { t.MedicamentoId, t.Horario, t.Estado, RegistradoPor = t.RegistradoPor!.NombreCompleto, t.FechaRegistro })
            .ToListAsync();

        var agenda = medicamentos
            .SelectMany(m => m.Horarios.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(h =>
                {
                    var toma = tomas.FirstOrDefault(t => t.MedicamentoId == m.Id && t.Horario == h);
                    return new
                    {
                        MedicamentoId = m.Id,
                        m.PersonaAsistidaId,
                        m.PersonaAsistida,
                        m.Nombre,
                        m.Dosis,
                        m.Indicaciones,
                        Horario = h,
                        Estado = toma?.Estado,
                        RegistradoPor = toma?.RegistradoPor,
                        FechaRegistro = Formato.Utc(toma?.FechaRegistro),
                    };
                }))
            .OrderBy(t => t.Horario)
            .ThenBy(t => t.PersonaAsistida)
            .ToList();

        return Ok(new { Fecha = dia.ToString("yyyy-MM-dd"), Tomas = agenda });
    }

    [HttpPut("{id}/tomas")]
    public async Task<IActionResult> RegistrarToma(int id, RegistrarTomaDto datos)
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return Unauthorized();

        var medicamento = await BuscarAsync(usuarioId, id, paraRegistrar: true);
        if (medicamento == null)
        {
            var loVe = await _context.Medicamentos.AnyAsync(m => m.Id == id && _context.IdsPersonasDe(usuarioId).Contains(m.PersonaAsistidaId));
            return loVe
                ? StatusCode(StatusCodes.Status403Forbidden, new { message = "Estás en modo observador: las tomas las registran los cuidadores." })
                : NotFound(new { message = "No se encontró el medicamento." });
        }

        var horarios = medicamento.Horarios.Split(',', StringSplitOptions.TrimEntries);
        if (!horarios.Contains(datos.Horario))
            return BadRequest(new { message = "Ese horario no está en la indicación del medicamento." });

        if (datos.Estado != null && datos.Estado != EstadosToma.Tomada && datos.Estado != EstadosToma.Omitida)
            return BadRequest(new { message = "El estado no es válido." });

        var dia = datos.Fecha.Date;
        if (dia > DateTime.Today)
            return BadRequest(new { message = "No se pueden registrar tomas de días futuros." });

        var toma = await _context.TomasMedicamentos
            .FirstOrDefaultAsync(t => t.MedicamentoId == id && t.Fecha == dia && t.Horario == datos.Horario);

        if (datos.Estado == null)
        {
            if (toma != null) _context.TomasMedicamentos.Remove(toma);
        }
        else
        {
            if (toma == null)
            {
                toma = new TomaMedicamento { MedicamentoId = id, Fecha = dia, Horario = datos.Horario };
                _context.TomasMedicamentos.Add(toma);
            }

            toma.Estado = datos.Estado;
            toma.RegistradoPorId = usuarioId;
            toma.FechaRegistro = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { Estado = datos.Estado });
    }

    // para editar la medicación hay que poder gestionar a la persona; para registrar tomas, poder registrar
    private Task<Medicamento?> BuscarAsync(string usuarioId, int id, bool paraRegistrar = false)
    {
        var personasIds = paraRegistrar
            ? _context.IdsPersonasDondeRegistra(usuarioId)
            : _context.IdsPersonasQueGestiona(usuarioId);
        return _context.Medicamentos.FirstOrDefaultAsync(m => m.Id == id && personasIds.Contains(m.PersonaAsistidaId));
    }

    // "8:00, 20:00" → "08:00,20:00"; null si algún horario no es válido
    public static string? NormalizarHorarios(string texto)
    {
        var horarios = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var parte in texto.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TimeOnly.TryParseExact(parte, new[] { "H:mm", "HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
                return null;
            horarios.Add(hora.ToString("HH:mm", CultureInfo.InvariantCulture));
        }

        return horarios.Count == 0 ? null : string.Join(',', horarios);
    }

    private static void CopiarDatos(MedicamentoDto origen, Medicamento destino, string horarios)
    {
        destino.Nombre = origen.Nombre.Trim();
        destino.Dosis = Limpiar(origen.Dosis);
        destino.Horarios = horarios;
        destino.Indicaciones = Limpiar(origen.Indicaciones);
        destino.Medico = Limpiar(origen.Medico);
        destino.VencimientoReceta = origen.VencimientoReceta?.Date;
        destino.Activo = origen.Activo;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
