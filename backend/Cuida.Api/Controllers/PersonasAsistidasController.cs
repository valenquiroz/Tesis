using Cuida.Api.Data;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cuidador")]
public class PersonasAsistidasController : ControllerBase
{
    public const string OrigenVencimientoCud = "cud-vencimiento";

    private readonly ApplicationDbContext _context;
    private readonly AlmacenamientoDocumentos _almacenamiento;
    private readonly UserManager<ApplicationUser> _userManager;

    public PersonasAsistidasController(
        ApplicationDbContext context,
        AlmacenamientoDocumentos almacenamiento,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _almacenamiento = almacenamiento;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetPersonasAsistidas()
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var personas = await _context.PersonasDe(usuarioId)
            .AsNoTracking()
            .OrderBy(p => p.NombreCompleto)
            .ToListAsync();

        await _context.CompletarAccesoAsync(usuarioId, personas);

        return Ok(personas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPersonaAsistida(int id)
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var persona = await _context.PersonasDe(usuarioId)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (persona == null)
        {
            return NotFound(new
            {
                message = "No se encontró la persona asistida."
            });
        }

        await _context.CompletarAccesoAsync(usuarioId, new[] { persona });

        return Ok(persona);
    }

    // Una familia la carga en su cuenta; un gerente, como paciente de su institución
    [HttpPost]
    public async Task<IActionResult> Crear(PersonaAsistidaRequest request)
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
        {
            return Unauthorized(new
            {
                message = "No se pudo identificar al usuario."
            });
        }

        var yo = await _context.Users.FirstAsync(u => u.Id == usuarioId);
        var persona = new PersonaAsistida { ResponsableId = usuarioId };

        var plan = Planes.De(yo.Plan);
        var propias = await _context.PersonasAsistidas
            .CountAsync(p => p.ResponsableId == usuarioId);

        if (plan.Personas is int maximo && propias >= maximo)
        {
            return BadRequest(new
            {
                message = maximo == 1
                    ? $"El plan Básico incluye una persona. Para sumar otra, pasate al plan {Planes.QueIncluye(l => l.Personas is null or > 1)}."
                    : $"Tu plan incluye hasta {maximo} personas. Para sumar más, pasate al plan {Planes.QueIncluye(l => l.Personas is null)}.",
                codigo = "plan"
            });
        }

        CopiarDatos(request, persona);

        _context.PersonasAsistidas.Add(persona);
        await _context.SaveChangesAsync();

        await SincronizarAvisoCudAsync(persona);
        await _context.CompletarAccesoAsync(usuarioId, new[] { persona });

        return CreatedAtAction(
            nameof(GetPersonaAsistida),
            new { id = persona.Id },
            persona);
    }

    // perfil y ficha: solo quien la administra (los cuidadores y observadores la ven)
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarPersonaAsistida(
        int id,
        PersonaAsistidaRequest datos)
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var persona = await _context.PersonasQueAdministra(usuarioId).FirstOrDefaultAsync(p => p.Id == id);

        if (persona == null)
        {
            return await _context.PuedeVerAsync(usuarioId, id)
                ? SinPermiso("El perfil y la ficha los edita quien es responsable de la persona.")
                : NotFound(new { message = "No se encontró la persona asistida." });
        }

        CopiarDatos(datos, persona);

        await _context.SaveChangesAsync();

        await SincronizarAvisoCudAsync(persona);
        await _context.CompletarAccesoAsync(usuarioId, new[] { persona });

        return Ok(persona);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarPersonaAsistida(int id)
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var persona = await _context.PersonasQueAdministra(usuarioId).FirstOrDefaultAsync(p => p.Id == id);

        if (persona == null)
        {
            return await _context.PuedeVerAsync(usuarioId, id)
                ? SinPermiso("Solo quien es responsable de la persona puede eliminarla. Si ya no la acompañás, podés salir del equipo.")
                : NotFound(new { message = "No se encontró la persona asistida." });
        }

        // los documentos se borran en cascada pero los archivos quedan en disco
        var rutasArchivos = await _context.Documentos
            .Where(d => d.PersonaAsistidaId == id)
            .Select(d => d.RutaArchivo)
            .ToListAsync();

        _context.PersonasAsistidas.Remove(persona);

        await _context.SaveChangesAsync();

        foreach (var ruta in rutasArchivos)
        {
            _almacenamiento.Eliminar(ruta);
        }

        return Ok(new
        {
            message = "Persona asistida eliminada correctamente."
        });
    }

    // ------------------------------------------------------------------
    // Equipo de cuidado: cuidadores (registran) y observadores (miran)
    // ------------------------------------------------------------------

    [HttpGet("{id}/equipo")]
    public async Task<IActionResult> GetEquipo(int id)
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var persona = await _context.PersonasDe(usuarioId)
            .AsNoTracking()
            .Include(p => p.Responsable)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (persona == null)
            return NotFound(new { message = "No se encontró la persona asistida." });

        await _context.CompletarAccesoAsync(usuarioId, new[] { persona });

        var miembros = await _context.PersonasCompartidas
            .AsNoTracking()
            .Where(c => c.PersonaAsistidaId == id)
            .OrderBy(c => c.FechaAlta)
            .Select(c => new
            {
                c.UsuarioId,
                c.Usuario!.NombreCompleto,
                c.Usuario.Email,
                c.Rol,
                FechaAlta = Formato.Utc(c.FechaAlta),
            })
            .ToListAsync();

        var limites = Planes.De(persona.Plan);
        var usados = await _context.IntegrantesIds(persona.ResponsableId).CountAsync();

        return Ok(new
        {
            SoyResponsable = persona.PuedeAdministrar,
            persona.ResponsableCuida,
            Responsable = new
            {
                persona.ResponsableId,
                Nombre = persona.Responsable?.NombreGrupo ?? persona.Responsable?.NombreCompleto,
                persona.Responsable?.Email,
            },
            Miembros = miembros,
            Plan = new
            {
                limites.Codigo,
                limites.Nombre,
                limites.Integrantes,
                limites.Grupo,
                Usados = usados,
            },
        });
    }

    [HttpPost("{id}/equipo")]
    public async Task<IActionResult> SumarAlEquipo(int id, SumarAlEquipoDto datos)
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        if (!NivelesAcceso.DelEquipo.Contains(datos.Rol))
            return BadRequest(new { message = "El rol tiene que ser Cuidador u Observador." });

        var persona = await _context.PersonasQueAdministra(usuarioId).FirstOrDefaultAsync(p => p.Id == id);

        if (persona == null)
        {
            return NotFound(new
            {
                message = "Solo quien es responsable de la persona puede armar su equipo."
            });
        }

        var invitado = await _userManager.FindByEmailAsync(datos.Email.Trim());

        // mismo mensaje si no existe o no es una cuenta particular: no revela qué correos están registrados
        if (invitado == null || !await _userManager.IsInRoleAsync(invitado, "Cuidador"))
        {
            return BadRequest(new
            {
                message = "No encontramos una cuenta con ese correo. Pedile que se registre primero."
            });
        }

        if (invitado.Id == persona.ResponsableId || invitado.Id == usuarioId)
            return BadRequest(new { message = "Esa cuenta ya es la responsable de la persona." });

        var yaEsta = await _context.PersonasCompartidas
            .AnyAsync(c => c.PersonaAsistidaId == id && c.UsuarioId == invitado.Id);

        if (yaEsta)
            return BadRequest(new { message = $"{invitado.NombreCompleto} ya está en el equipo." });

        var motivo = await _context.MotivoParaNoSumarAsync(persona.ResponsableId, invitado.Id);
        if (motivo != null)
            return BadRequest(new { message = motivo, codigo = "plan" });

        _context.PersonasCompartidas.Add(new PersonaCompartida
        {
            PersonaAsistidaId = id,
            UsuarioId = invitado.Id,
            Rol = datos.Rol,
            FechaAlta = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = datos.Rol == NivelesAcceso.Cuidador
                ? $"Listo: {invitado.NombreCompleto} ya puede registrar los cuidados de {persona.NombreCompleto}."
                : $"Listo: {invitado.NombreCompleto} ya puede ver lo que se registra de {persona.NombreCompleto}."
        });
    }

    [HttpPut("{id}/equipo/{usuarioId}")]
    public async Task<IActionResult> CambiarRol(int id, string usuarioId, CambiarRolDto datos)
    {
        var yo = User.UsuarioId();

        if (string.IsNullOrEmpty(yo))
            return Unauthorized();

        if (!NivelesAcceso.DelEquipo.Contains(datos.Rol))
            return BadRequest(new { message = "El rol tiene que ser Cuidador u Observador." });

        var persona = await _context.PersonasQueAdministra(yo).FirstOrDefaultAsync(p => p.Id == id);

        if (persona == null)
            return NotFound(new { message = "Solo quien es responsable de la persona puede cambiar el equipo." });

        var miembro = await _context.PersonasCompartidas
            .FirstOrDefaultAsync(c => c.PersonaAsistidaId == id && c.UsuarioId == usuarioId);

        if (miembro == null)
            return NotFound(new { message = "Esa persona no está en el equipo." });

        miembro.Rol = datos.Rol;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Se actualizó el rol." });
    }

    // el responsable saca a cualquiera; los demás solo pueden salir ellos
    [HttpDelete("{id}/equipo/{usuarioId}")]
    public async Task<IActionResult> QuitarDelEquipo(int id, string usuarioId)
    {
        var yo = User.UsuarioId();

        if (string.IsNullOrEmpty(yo))
            return Unauthorized();

        var persona = await _context.PersonaDeAsync(yo, id);

        if (persona == null)
            return NotFound(new { message = "No se encontró la persona asistida." });

        var administra = await _context.PersonasQueAdministra(yo).AnyAsync(p => p.Id == id);

        if (!administra && usuarioId != yo)
            return SinPermiso("Solo quien es responsable de la persona puede quitar a otros del equipo.");

        var miembro = await _context.PersonasCompartidas
            .FirstOrDefaultAsync(c => c.PersonaAsistidaId == id && c.UsuarioId == usuarioId);

        if (miembro == null)
            return NotFound(new { message = "Esa persona no está en el equipo." });

        _context.PersonasCompartidas.Remove(miembro);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = usuarioId == yo
                ? "Saliste del equipo de esta persona."
                : "Se quitó a la persona del equipo."
        });
    }

    // "Yo también la cuido": si se apaga, el responsable pasa a modo observador
    [HttpPut("{id}/responsable-cuida")]
    public async Task<IActionResult> CambiarResponsableCuida(int id, ResponsableCuidaDto datos)
    {
        var usuarioId = User.UsuarioId();

        if (string.IsNullOrEmpty(usuarioId))
            return Unauthorized();

        var persona = await _context.PersonasAsistidas
            .Include(p => p.Responsable)
            .FirstOrDefaultAsync(p => p.Id == id && p.ResponsableId == usuarioId);

        if (persona == null)
            return NotFound(new { message = "No se encontró la persona asistida." });

        if (!datos.ResponsableCuida && !Planes.De(persona.Responsable?.Plan).Grupo)
        {
            return BadRequest(new
            {
                message = $"Para solo mirar lo que registran otros, pasate al plan {Planes.QueIncluye(l => l.Grupo)}.",
                codigo = "plan"
            });
        }

        persona.ResponsableCuida = datos.ResponsableCuida;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = datos.ResponsableCuida
                ? "Listo: vas a poder registrar los cuidados de esta persona."
                : "Listo: vas a ver lo que registran los cuidadores, sin poder cambiarlo."
        });
    }

    // ------------------------------------------------------------------

    private ObjectResult SinPermiso(string mensaje) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = mensaje });

    // Con fecha de vencimiento del CUD se agenda un aviso dos meses antes
    private async Task SincronizarAvisoCudAsync(PersonaAsistida persona)
    {
        var aviso = await _context.Turnos
            .FirstOrDefaultAsync(t => t.PersonaAsistidaId == persona.Id && t.Origen == OrigenVencimientoCud);

        if (persona.VencimientoCud is not DateTime vence)
        {
            if (aviso != null)
            {
                _context.Turnos.Remove(aviso);
                await _context.SaveChangesAsync();
            }
            return;
        }

        var fecha = vence.Date.AddDays(-60);
        if (fecha < DateTime.Today)
            fecha = vence.Date;

        if (aviso == null)
        {
            aviso = new Turno
            {
                PersonaAsistidaId = persona.Id,
                CuidadorId = persona.ResponsableId,
                Origen = OrigenVencimientoCud,
            };
            _context.Turnos.Add(aviso);
        }

        var nombre = persona.NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        aviso.Titulo = $"Renovar el CUD de {nombre}";
        aviso.FechaHora = fecha.AddHours(9);
        aviso.Especialidad = "Trámite";
        aviso.Lugar = "Junta evaluadora";
        aviso.Observaciones = $"El certificado vence el {vence:dd/MM/yyyy}. Para renovarlo hay que volver a la junta con el certificado vencido.";

        await _context.SaveChangesAsync();
    }

    private static void CopiarDatos(PersonaAsistidaRequest origen, PersonaAsistida destino)
    {
        destino.NombreCompleto = origen.NombreCompleto.Trim();
        destino.FechaNacimiento = origen.FechaNacimiento;
        destino.RelacionConCuidador = Limpiar(origen.RelacionConCuidador);
        destino.TipoDiscapacidad = Limpiar(origen.TipoDiscapacidad);
        destino.ObraSocial = Limpiar(origen.ObraSocial);
        destino.Localidad = Limpiar(origen.Localidad);
        destino.Domicilio = Limpiar(origen.Domicilio);
        destino.Observaciones = Limpiar(origen.Observaciones);
        destino.Necesidades = Limpiar(origen.Necesidades);
        destino.GrupoSanguineo = Limpiar(origen.GrupoSanguineo);
        destino.Alergias = Limpiar(origen.Alergias);
        destino.Diagnostico = Limpiar(origen.Diagnostico);
        destino.NumeroAfiliado = Limpiar(origen.NumeroAfiliado);
        destino.MedicoCabecera = Limpiar(origen.MedicoCabecera);
        destino.TelefonoMedico = Limpiar(origen.TelefonoMedico);
        destino.InformacionEmergencia = Limpiar(origen.InformacionEmergencia);
        destino.VencimientoCud = origen.VencimientoCud?.Date;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
