using System.Globalization;
using Cuida.Api.Data;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Controllers;

// Panel del dueño de un grupo (una familia, una clínica): sus personas, quiénes las cuidan o
// miran, y todo lo que registra cada uno. Se mira por persona o por quien ayuda.
// - Plan Equipo: lo registrado y lo que quedó sin registrar en la última semana.
// - Plan Plus: además, el cumplimiento de cada cuidador (tomas a tiempo, tarde) y hasta 31 días.
// El dueño no cambia lo que anotó otro.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cuidador")]
public class GrupoController : ControllerBase
{
    // una toma registrada más de una hora después de su horario cuenta como tardía
    private const int MinutosTarde = 60;

    // sin supervisión se mira solo la última semana
    private const int DiasSinSupervision = 7;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GrupoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();

        return Ok(new
        {
            yo.NombreGrupo,
            Plan = Planes.De(yo.Plan),
            Personas = await _context.PersonasAsistidas
                .AsNoTracking()
                .Where(p => p.ResponsableId == yo.Id)
                .OrderBy(p => p.NombreCompleto)
                .Select(p => new { p.Id, p.NombreCompleto })
                .ToListAsync(),
            Integrantes = await _context.IntegrantesIds(yo.Id).CountAsync(),
            // grupos de otros dueños en los que ayuda (una cuidadora de una residencia)
            DondeAyuda = await _context.GruposDondeAyudaAsync(yo.Id),
        });
    }

    // ------------------------------------------------------------------
    // Por persona
    // ------------------------------------------------------------------

    [HttpGet("personas")]
    public async Task<IActionResult> GetPersonas([FromQuery] int dias = 7)
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();
        var limites = Planes.De(yo.Plan);
        if (!limites.Grupo) return SinPlan(l => l.Grupo, "Ver lo que registra cada uno");
        dias = Dias(limites, dias);

        var personas = await _context.PersonasAsistidas
            .AsNoTracking()
            .Where(p => p.ResponsableId == yo.Id)
            .OrderBy(p => p.NombreCompleto)
            .Select(p => new { p.Id, p.NombreCompleto, p.TipoDiscapacidad, p.ResponsableCuida })
            .ToListAsync();

        var ids = personas.Select(p => p.Id).ToList();
        var integrantes = await IntegrantesDeAsync(yo.Id, ids);
        var tomas = await TomasProgramadasAsync(ids, Desde(dias));
        var registros = await RegistrosAsync(ids, Desde(dias));

        return Ok(new
        {
            Dias = dias,
            limites.Supervision,
            Personas = personas.Select(p =>
            {
                var suyas = tomas.Where(t => t.PersonaId == p.Id).ToList();
                var ultimo = registros.Where(r => r.PersonaId == p.Id).MaxBy(r => r.Anotado);
                return new
                {
                    p.Id,
                    p.NombreCompleto,
                    p.TipoDiscapacidad,
                    TeCuida = p.ResponsableCuida,
                    Integrantes = integrantes.Where(i => i.PersonaId == p.Id).Select(i => new { i.Id, i.Nombre, i.Rol }),
                    Programadas = suyas.Count,
                    Registradas = suyas.Count(t => t.Estado != null),
                    SinRegistrar = suyas.Count(t => t.Estado == null),
                    Tarde = limites.Supervision ? suyas.Count(t => t.DemoraMinutos > MinutosTarde) : (int?)null,
                    Registros = registros.Count(r => r.PersonaId == p.Id),
                    Ultimo = ultimo == null ? null : new { ultimo.Autor, ultimo.Anotado, ultimo.Tipo },
                };
            }),
        });
    }

    [HttpGet("personas/{personaId:int}")]
    public async Task<IActionResult> GetPersona(int personaId, [FromQuery] int dias = 7)
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();
        var limites = Planes.De(yo.Plan);
        if (!limites.Grupo) return SinPlan(l => l.Grupo, "Ver lo que registra cada uno");
        dias = Dias(limites, dias);

        var persona = await _context.PersonasAsistidas
            .AsNoTracking()
            .Where(p => p.Id == personaId && p.ResponsableId == yo.Id)
            .Select(p => new { p.Id, p.NombreCompleto, p.TipoDiscapacidad, TeCuida = p.ResponsableCuida })
            .FirstOrDefaultAsync();

        if (persona == null)
            return NotFound(new { message = "Esa persona no es de tu grupo." });

        var ids = new List<int> { personaId };
        var integrantes = await IntegrantesDeAsync(yo.Id, ids);
        var tomas = await TomasProgramadasAsync(ids, Desde(dias));
        var registros = await RegistrosAsync(ids, Desde(dias));

        // cumplimiento de cada cuidador con esta persona (solo Plus)
        var porCuidador = limites.Supervision
            ? integrantes
                .Where(i => i.Rol == NivelesAcceso.Cuidador)
                .Select(i => new
                {
                    i.Id,
                    i.Nombre,
                    Registradas = tomas.Count(t => t.AutorId == i.Id),
                    Tarde = tomas.Count(t => t.AutorId == i.Id && t.DemoraMinutos > MinutosTarde),
                    Diario = registros.Count(r => r.AutorId == i.Id && r.Tipo == "Diario"),
                })
                .ToList()
            : null;

        return Ok(new
        {
            Dias = dias,
            limites.Supervision,
            Persona = persona,
            Integrantes = integrantes.Select(i => new { i.Id, i.Nombre, i.Rol }),
            Programadas = tomas.Count,
            Registradas = tomas.Count(t => t.Estado != null),
            SinRegistrar = tomas.Where(t => t.Estado == null).OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Horario).Take(50),
            PorCuidador = porCuidador,
            Registros = registros.OrderByDescending(r => r.Momento).Take(200),
        });
    }

    // ------------------------------------------------------------------
    // Por quien ayuda
    // ------------------------------------------------------------------

    [HttpGet("integrantes")]
    public async Task<IActionResult> GetIntegrantes([FromQuery] int dias = 7)
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();
        var limites = Planes.De(yo.Plan);
        dias = Dias(limites, dias);

        var misPersonas = await _context.PersonasAsistidas.Where(p => p.ResponsableId == yo.Id).Select(p => p.Id).ToListAsync();
        var asignaciones = await IntegrantesDeAsync(yo.Id, misPersonas);
        var tomas = limites.Grupo ? await TomasProgramadasAsync(misPersonas, Desde(dias)) : new List<TomaProgramada>();
        var registros = limites.Grupo ? await RegistrosAsync(misPersonas, Desde(dias)) : new List<Registro>();

        var resultado = asignaciones
            .GroupBy(a => a.Id)
            .OrderBy(g => g.First().Nombre)
            .Select(g =>
            {
                var cuida = g.Where(a => a.Rol == NivelesAcceso.Cuidador).Select(a => a.PersonaId).ToHashSet();
                var suyas = tomas.Where(t => cuida.Contains(t.PersonaId)).ToList();
                var propios = registros.Where(r => r.AutorId == g.Key).ToList();
                return new
                {
                    g.First().Id,
                    NombreCompleto = g.First().Nombre,
                    g.First().Email,
                    Personas = g.OrderBy(a => a.Persona).Select(a => new { Id = a.PersonaId, Nombre = a.Persona, a.Rol }),
                    Registros = propios.Count,
                    UltimaActividad = propios.Select(r => (DateTime?)r.Anotado).Max(),
                    Actividad = limites.Supervision && cuida.Count > 0
                        ? new
                        {
                            Programadas = suyas.Count,
                            Registradas = suyas.Count(t => t.AutorId == g.Key),
                            SinRegistrar = suyas.Count(t => t.Estado == null),
                            Tarde = suyas.Count(t => t.AutorId == g.Key && t.DemoraMinutos > MinutosTarde),
                        }
                        : null,
                };
            })
            .ToList();

        return Ok(new { Dias = dias, limites.Supervision, Integrantes = resultado });
    }

    [HttpGet("integrantes/{integranteId}")]
    public async Task<IActionResult> GetIntegrante(string integranteId, [FromQuery] int dias = 7)
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();
        var limites = Planes.De(yo.Plan);
        if (!limites.Grupo) return SinPlan(l => l.Grupo, "Ver lo que registra cada uno");
        dias = Dias(limites, dias);

        var misPersonas = await _context.PersonasAsistidas.Where(p => p.ResponsableId == yo.Id).Select(p => p.Id).ToListAsync();
        var asignaciones = (await IntegrantesDeAsync(yo.Id, misPersonas)).Where(a => a.Id == integranteId).ToList();

        if (asignaciones.Count == 0)
            return NotFound(new { message = "Esa persona no está en tu grupo." });

        var cuida = asignaciones.Where(a => a.Rol == NivelesAcceso.Cuidador).Select(a => a.PersonaId).ToList();
        var tomas = await TomasProgramadasAsync(cuida, Desde(dias));
        var registros = (await RegistrosAsync(misPersonas, Desde(dias))).Where(r => r.AutorId == integranteId).ToList();

        var porPersona = limites.Supervision
            ? asignaciones
                .Where(a => a.Rol == NivelesAcceso.Cuidador)
                .Select(a =>
                {
                    var suyas = tomas.Where(t => t.PersonaId == a.PersonaId).ToList();
                    return new
                    {
                        Id = a.PersonaId,
                        Nombre = a.Persona,
                        Programadas = suyas.Count,
                        RegistradasPorCuidador = suyas.Count(t => t.AutorId == integranteId),
                        RegistradasPorOtros = suyas.Count(t => t.Estado != null && t.AutorId != integranteId),
                        SinRegistrar = suyas.Count(t => t.Estado == null),
                        Tarde = suyas.Count(t => t.AutorId == integranteId && t.DemoraMinutos > MinutosTarde),
                    };
                })
                .ToList()
            : null;

        return Ok(new
        {
            Dias = dias,
            limites.Supervision,
            Integrante = new { asignaciones[0].Id, NombreCompleto = asignaciones[0].Nombre, asignaciones[0].Email },
            Personas = asignaciones.OrderBy(a => a.Persona).Select(a => new { Id = a.PersonaId, Nombre = a.Persona, a.Rol }),
            PorPersona = porPersona,
            SinRegistrar = tomas.Where(t => t.Estado == null).OrderByDescending(t => t.Fecha).ThenByDescending(t => t.Horario).Take(50),
            Registros = registros.OrderByDescending(r => r.Momento).Take(200),
        });
    }

    // suma a alguien que ya tiene cuenta, para cuidar o mirar a una o varias personas
    [HttpPost("integrantes")]
    public async Task<IActionResult> Sumar(SumarAlGrupoDto datos)
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();

        if (!NivelesAcceso.DelEquipo.Contains(datos.Rol))
            return BadRequest(new { message = "El rol tiene que ser Cuidador u Observador." });

        var ids = datos.PersonasIds.Distinct().ToList();
        if (ids.Count == 0)
            return BadRequest(new { message = "Elegí al menos una persona." });

        var personas = await _context.PersonasAsistidas
            .Where(p => p.ResponsableId == yo.Id && ids.Contains(p.Id))
            .ToListAsync();

        if (personas.Count != ids.Count)
            return NotFound(new { message = "Alguna de esas personas no es de tu grupo." });

        var invitado = await _userManager.FindByEmailAsync(datos.Email.Trim());

        // mismo mensaje si no existe: no revela qué correos están registrados
        if (invitado == null || !await _userManager.IsInRoleAsync(invitado, "Cuidador"))
            return BadRequest(new { message = "No encontramos una cuenta con ese correo. Pedile que se registre primero." });

        if (invitado.Id == yo.Id)
            return BadRequest(new { message = "Ese es tu correo: el grupo ya es tuyo." });

        var motivo = await _context.MotivoParaNoSumarAsync(yo.Id, invitado.Id);
        if (motivo != null)
            return BadRequest(new { message = motivo, codigo = "plan" });

        var existentes = await _context.PersonasCompartidas
            .Where(c => c.UsuarioId == invitado.Id && ids.Contains(c.PersonaAsistidaId))
            .ToListAsync();

        foreach (var persona in personas)
        {
            var existente = existentes.FirstOrDefault(c => c.PersonaAsistidaId == persona.Id);
            if (existente != null)
            {
                existente.Rol = datos.Rol;
                continue;
            }

            _context.PersonasCompartidas.Add(new PersonaCompartida
            {
                PersonaAsistidaId = persona.Id,
                UsuarioId = invitado.Id,
                Rol = datos.Rol,
                FechaAlta = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();

        var nombres = string.Join(", ", personas.Select(p => p.NombreCompleto));
        return Ok(new
        {
            message = datos.Rol == NivelesAcceso.Cuidador
                ? $"Listo: {invitado.NombreCompleto} ya puede registrar los cuidados de {nombres}."
                : $"Listo: {invitado.NombreCompleto} ya puede ver lo que se registra de {nombres}."
        });
    }

    // cambia a quiénes acompaña y con qué rol (lo que no está en la lista deja de verlo)
    [HttpPut("integrantes/{integranteId}")]
    public async Task<IActionResult> CambiarAsignacion(string integranteId, AsignacionDto datos)
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();

        if (!NivelesAcceso.DelEquipo.Contains(datos.Rol))
            return BadRequest(new { message = "El rol tiene que ser Cuidador u Observador." });

        var ids = datos.PersonasIds.Distinct().ToList();
        if (ids.Count == 0)
            return BadRequest(new { message = "Elegí al menos una persona. Para que deje de ver a todas, quitalo del grupo." });

        var misPersonas = await _context.PersonasAsistidas.Where(p => p.ResponsableId == yo.Id).Select(p => p.Id).ToListAsync();
        if (ids.Any(id => !misPersonas.Contains(id)))
            return NotFound(new { message = "Alguna de esas personas no es de tu grupo." });

        var actuales = await _context.PersonasCompartidas
            .Where(c => c.UsuarioId == integranteId && misPersonas.Contains(c.PersonaAsistidaId))
            .ToListAsync();

        if (actuales.Count == 0)
            return NotFound(new { message = "Esa persona no está en tu grupo." });

        _context.PersonasCompartidas.RemoveRange(actuales.Where(c => !ids.Contains(c.PersonaAsistidaId)));
        foreach (var c in actuales.Where(c => ids.Contains(c.PersonaAsistidaId)))
            c.Rol = datos.Rol;
        foreach (var id in ids.Where(id => actuales.All(c => c.PersonaAsistidaId != id)))
        {
            _context.PersonasCompartidas.Add(new PersonaCompartida
            {
                PersonaAsistidaId = id,
                UsuarioId = integranteId,
                Rol = datos.Rol,
                FechaAlta = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Se actualizó a quiénes acompaña." });
    }

    // lo saca de todas las personas del grupo
    [HttpDelete("integrantes/{integranteId}")]
    public async Task<IActionResult> Quitar(string integranteId)
    {
        var yo = await YoAsync();
        if (yo == null) return Unauthorized();

        var asignaciones = await _context.PersonasCompartidas
            .Include(c => c.Usuario)
            .Where(c => c.UsuarioId == integranteId && c.PersonaAsistida!.ResponsableId == yo.Id)
            .ToListAsync();

        if (asignaciones.Count == 0)
            return NotFound(new { message = "Esa persona no está en tu grupo." });

        var nombre = asignaciones[0].Usuario?.NombreCompleto;
        _context.PersonasCompartidas.RemoveRange(asignaciones);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"{nombre} ya no es parte de tu grupo." });
    }

    // ------------------------------------------------------------------

    private async Task<ApplicationUser?> YoAsync()
    {
        var usuarioId = User.UsuarioId();
        if (string.IsNullOrEmpty(usuarioId)) return null;

        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
    }

    private static int Dias(LimitesPlan limites, int pedidos) =>
        limites.Supervision ? Math.Clamp(pedidos, 1, 31) : DiasSinSupervision;

    private static DateTime Desde(int dias) => DateTime.Today.AddDays(-(dias - 1));

    private BadRequestObjectResult SinPlan(Func<LimitesPlan, bool> incluye, string que) =>
        BadRequest(new { message = $"{que} está desde el plan {Planes.QueIncluye(incluye)}.", codigo = "plan" });

    private record Asignacion(string Id, string Nombre, string? Email, int PersonaId, string Persona, string Rol);

    private async Task<List<Asignacion>> IntegrantesDeAsync(string duenoId, List<int> personasIds) =>
        await _context.PersonasCompartidas
            .AsNoTracking()
            .Where(c => c.PersonaAsistida!.ResponsableId == duenoId && personasIds.Contains(c.PersonaAsistidaId))
            .OrderBy(c => c.Usuario!.NombreCompleto)
            .Select(c => new Asignacion(c.UsuarioId, c.Usuario!.NombreCompleto, c.Usuario.Email,
                c.PersonaAsistidaId, c.PersonaAsistida!.NombreCompleto, c.Rol))
            .ToListAsync();

    // Cada toma que tocaba dar en el período (según los horarios de la medicación activa) con lo
    // que quedó registrado: quién la anotó y con cuánta demora. Estado null = sin registrar.
    public record TomaProgramada(int PersonaId, string Persona, string Medicamento, string Fecha, string Horario,
        string? Estado, string? AutorId, string? Autor, DateTime? Anotado, int? DemoraMinutos);

    private async Task<List<TomaProgramada>> TomasProgramadasAsync(List<int> personasIds, DateTime desde)
    {
        var hoy = DateTime.Today;
        var horaActual = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);

        var medicamentos = await _context.Medicamentos
            .AsNoTracking()
            .Where(m => personasIds.Contains(m.PersonaAsistidaId) && m.Activo)
            .Select(m => new { m.Id, m.PersonaAsistidaId, Persona = m.PersonaAsistida!.NombreCompleto, m.Nombre, m.Horarios, m.FechaCreacion })
            .ToListAsync();

        var medsIds = medicamentos.Select(m => m.Id).ToList();
        var tomas = await _context.TomasMedicamentos
            .AsNoTracking()
            .Where(t => medsIds.Contains(t.MedicamentoId) && t.Fecha >= desde)
            .Select(t => new { t.MedicamentoId, t.Fecha, t.Horario, t.Estado, t.RegistradoPorId, RegistradoPor = t.RegistradoPor!.NombreCompleto, t.FechaRegistro })
            .ToListAsync();

        var resultado = new List<TomaProgramada>();
        foreach (var m in medicamentos)
        {
            var inicio = m.FechaCreacion.ToLocalTime().Date;
            var horarios = m.Horarios.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (var dia = desde > inicio ? desde : inicio; dia <= hoy; dia = dia.AddDays(1))
            {
                foreach (var h in horarios)
                {
                    if (dia == hoy && string.CompareOrdinal(h, horaActual) > 0) continue;

                    var toma = tomas.FirstOrDefault(t => t.MedicamentoId == m.Id && t.Fecha == dia && t.Horario == h);
                    int? demora = null;
                    if (toma != null)
                    {
                        var programada = dia.Add(TimeSpan.Parse(h, CultureInfo.InvariantCulture));
                        demora = (int)Math.Round((toma.FechaRegistro.ToLocalTime() - programada).TotalMinutes);
                    }

                    resultado.Add(new TomaProgramada(m.PersonaAsistidaId, m.Persona, m.Nombre, dia.ToString("yyyy-MM-dd"), h,
                        toma?.Estado, toma?.RegistradoPorId, toma?.RegistradoPor, Formato.Utc(toma?.FechaRegistro), demora));
                }
            }
        }

        return resultado;
    }

    // Todo lo que se registró de esas personas en el período: tomas, diario y documentos, con su autor.
    // Momento es la hora local a la que se refiere el registro; Anotado, cuándo se cargó (UTC).
    public record Registro(string Tipo, string Momento, DateTime Anotado, int PersonaId, string Persona,
        string AutorId, string? Autor, string Titulo, string? Detalle, string? Estado, int? DemoraMinutos);

    private async Task<List<Registro>> RegistrosAsync(List<int> personasIds, DateTime desde)
    {
        var desdeUtc = desde.ToUniversalTime();

        var tomas = await _context.TomasMedicamentos
            .AsNoTracking()
            .Where(t => personasIds.Contains(t.Medicamento!.PersonaAsistidaId) && t.Fecha >= desde)
            .Select(t => new
            {
                t.Fecha, t.Horario, t.Estado, t.RegistradoPorId, Autor = t.RegistradoPor!.NombreCompleto, t.FechaRegistro,
                t.Medicamento!.PersonaAsistidaId, Persona = t.Medicamento.PersonaAsistida!.NombreCompleto,
                Medicamento = t.Medicamento.Nombre, t.Medicamento.Dosis,
            })
            .ToListAsync();

        var diario = await _context.EntradasDiario
            .AsNoTracking()
            .Where(e => personasIds.Contains(e.PersonaAsistidaId) && e.Fecha >= desde)
            .Select(e => new
            {
                e.Fecha, e.Tipo, e.Intensidad, e.Texto, e.AutorId, Autor = e.Autor!.NombreCompleto, e.FechaCreacion,
                e.PersonaAsistidaId, Persona = e.PersonaAsistida!.NombreCompleto,
            })
            .ToListAsync();

        var documentos = await _context.Documentos
            .AsNoTracking()
            .Where(d => personasIds.Contains(d.PersonaAsistidaId) && d.FechaCarga >= desdeUtc)
            .Select(d => new
            {
                d.NombreOriginal, d.CuidadorId, d.FechaCarga, d.PersonaAsistidaId, Persona = d.PersonaAsistida!.NombreCompleto,
            })
            .ToListAsync();

        var autoresDocumentos = await _context.Users
            .Where(u => documentos.Select(d => d.CuidadorId).Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.NombreCompleto);

        static string Local(DateTime fecha) => fecha.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

        var registros = new List<Registro>();

        foreach (var t in tomas)
        {
            var programada = t.Fecha.Add(TimeSpan.Parse(t.Horario, CultureInfo.InvariantCulture));
            var demora = (int)Math.Round((t.FechaRegistro.ToLocalTime() - programada).TotalMinutes);
            registros.Add(new Registro("Toma", Local(programada), Formato.Utc(t.FechaRegistro), t.PersonaAsistidaId, t.Persona,
                t.RegistradoPorId, t.Autor, t.Medicamento, t.Dosis, t.Estado, demora));
        }

        foreach (var e in diario)
        {
            registros.Add(new Registro("Diario", Local(e.Fecha), Formato.Utc(e.FechaCreacion), e.PersonaAsistidaId, e.Persona,
                e.AutorId, e.Autor, e.Intensidad is int i ? $"{e.Tipo} ({i}/5)" : e.Tipo, e.Texto, null, null));
        }

        foreach (var d in documentos)
        {
            registros.Add(new Registro("Documento", Local(d.FechaCarga.ToLocalTime()), Formato.Utc(d.FechaCarga), d.PersonaAsistidaId, d.Persona,
                d.CuidadorId, autoresDocumentos.GetValueOrDefault(d.CuidadorId), "Subió un documento", d.NombreOriginal, null, null));
        }

        return registros;
    }
}
