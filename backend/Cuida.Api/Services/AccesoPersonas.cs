using System.Security.Claims;
using Cuida.Api.Data;
using Cuida.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Services;

// Quién puede hacer qué con cada persona asistida. Todos los controladores pasan por acá
// para no repetir (ni olvidar) el filtro.
//
//  ver         el responsable (dueño del grupo) y su equipo (cuidadores y observadores)
//  gestionar   turnos, medicación, documentos, contactos, trámites: responsable y cuidadores
//  registrar   tomas y diario: cuidadores, y el responsable solo si también la cuida
//  administrar perfil, ficha, equipo, borrar: el responsable
public static class AccesoPersonas
{
    public static string? UsuarioId(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue(ClaimTypes.NameIdentifier);

    public static IQueryable<PersonaAsistida> PersonasDe(this ApplicationDbContext context, string usuarioId) =>
        context.PersonasAsistidas.Where(p =>
            p.ResponsableId == usuarioId ||
            context.PersonasCompartidas.Any(c => c.PersonaAsistidaId == p.Id && c.UsuarioId == usuarioId));

    public static IQueryable<PersonaAsistida> PersonasQueAdministra(this ApplicationDbContext context, string usuarioId) =>
        context.PersonasAsistidas.Where(p =>
            p.ResponsableId == usuarioId);

    public static IQueryable<PersonaAsistida> PersonasQueGestiona(this ApplicationDbContext context, string usuarioId) =>
        context.PersonasAsistidas.Where(p =>
            p.ResponsableId == usuarioId ||
            context.PersonasCompartidas.Any(c =>
                c.PersonaAsistidaId == p.Id && c.UsuarioId == usuarioId && c.Rol == NivelesAcceso.Cuidador));

    public static IQueryable<PersonaAsistida> PersonasDondeRegistra(this ApplicationDbContext context, string usuarioId) =>
        context.PersonasAsistidas.Where(p =>
            (p.ResponsableId == usuarioId && p.ResponsableCuida) ||
            context.PersonasCompartidas.Any(c =>
                c.PersonaAsistidaId == p.Id && c.UsuarioId == usuarioId && c.Rol == NivelesAcceso.Cuidador));

    public static IQueryable<int> IdsPersonasDe(this ApplicationDbContext context, string usuarioId) =>
        context.PersonasDe(usuarioId).Select(p => p.Id);

    public static IQueryable<int> IdsPersonasQueGestiona(this ApplicationDbContext context, string usuarioId) =>
        context.PersonasQueGestiona(usuarioId).Select(p => p.Id);

    public static IQueryable<int> IdsPersonasDondeRegistra(this ApplicationDbContext context, string usuarioId) =>
        context.PersonasDondeRegistra(usuarioId).Select(p => p.Id);

    public static Task<PersonaAsistida?> PersonaDeAsync(this ApplicationDbContext context, string usuarioId, int personaId) =>
        context.PersonasDe(usuarioId).FirstOrDefaultAsync(p => p.Id == personaId);

    public static Task<PersonaAsistida?> PersonaQueGestionaAsync(this ApplicationDbContext context, string usuarioId, int personaId) =>
        context.PersonasQueGestiona(usuarioId).FirstOrDefaultAsync(p => p.Id == personaId);

    public static Task<bool> PuedeVerAsync(this ApplicationDbContext context, string usuarioId, int personaId) =>
        context.PersonasDe(usuarioId).AnyAsync(p => p.Id == personaId);

    public static Task<bool> PuedeGestionarAsync(this ApplicationDbContext context, string usuarioId, int personaId) =>
        context.PersonasQueGestiona(usuarioId).AnyAsync(p => p.Id == personaId);

    public static Task<bool> PuedeRegistrarAsync(this ApplicationDbContext context, string usuarioId, int personaId) =>
        context.PersonasDondeRegistra(usuarioId).AnyAsync(p => p.Id == personaId);

    // --- planes ---

    // el plan que cubre a la persona es el de su responsable
    public static IQueryable<PersonaAsistida> ConPlan(this IQueryable<PersonaAsistida> personas, Func<LimitesPlan, bool> incluye)
    {
        var planes = Planes.Todos.Where(p => incluye(Planes.De(p))).ToList();
        return personas.Where(p => planes.Contains(p.Responsable!.Plan));
    }

    public static async Task<LimitesPlan> PlanDePersonaAsync(this ApplicationDbContext context, int personaId)
    {
        var plan = await context.PersonasAsistidas
            .Where(p => p.Id == personaId)
            .Select(p => p.Responsable!.Plan)
            .FirstOrDefaultAsync();

        return Planes.De(plan);
    }

    // Para lo que no depende de una persona (artículos, tu bienestar): el plan propio o el del
    // grupo en el que cuida, el que sea mejor. A quien cuida en una residencia Plus la cubre ese plan.
    public static async Task<LimitesPlan> PlanDelUsuarioAsync(this ApplicationDbContext context, string usuarioId)
    {
        var propio = await context.Users.Where(u => u.Id == usuarioId).Select(u => u.Plan).FirstOrDefaultAsync();
        var deSusGrupos = await context.PersonasDe(usuarioId).Select(p => p.Responsable!.Plan).Distinct().ToListAsync();

        return deSusGrupos.Select(Planes.De).Aggregate(Planes.De(propio), Planes.Mayor);
    }

    // Completa en cada persona qué puede hacer quien pregunta (para que la interfaz
    // muestre o esconda botones). El control real lo hacen las consultas de arriba.
    public static async Task CompletarAccesoAsync(this ApplicationDbContext context, string usuarioId, IEnumerable<PersonaAsistida> personas)
    {
        var lista = personas.ToList();
        if (lista.Count == 0) return;

        var ids = lista.Select(p => p.Id).ToList();
        var roles = await context.PersonasCompartidas
            .Where(c => c.UsuarioId == usuarioId && ids.Contains(c.PersonaAsistidaId))
            .ToDictionaryAsync(c => c.PersonaAsistidaId, c => c.Rol);

        var responsablesIds = lista.Select(p => p.ResponsableId).Distinct().ToList();
        var responsables = await context.Users
            .Where(u => responsablesIds.Contains(u.Id))
            .Select(u => new { u.Id, Nombre = u.NombreGrupo ?? u.NombreCompleto, u.Plan })
            .ToDictionaryAsync(u => u.Id);

        var propias = lista.Where(p => p.ResponsableId == usuarioId).Select(p => p.Id).ToList();
        var cuidadores = await context.PersonasCompartidas
            .Where(c => propias.Contains(c.PersonaAsistidaId) && c.Rol == NivelesAcceso.Cuidador)
            .OrderBy(c => c.Usuario!.NombreCompleto)
            .Select(c => new { c.PersonaAsistidaId, c.Usuario!.NombreCompleto })
            .ToListAsync();

        foreach (var p in lista)
        {
            var esResponsable = p.ResponsableId == usuarioId;
            p.Cuidadores = cuidadores.Where(c => c.PersonaAsistidaId == p.Id).Select(c => c.NombreCompleto).ToList();

            p.Acceso = esResponsable
                ? NivelesAcceso.Responsable
                : roles.GetValueOrDefault(p.Id, NivelesAcceso.Observador);
            p.EsTitular = esResponsable;
            p.NombreTitular = esResponsable ? null : responsables.GetValueOrDefault(p.ResponsableId)?.Nombre;
            p.PuedeAdministrar = esResponsable;
            p.PuedeGestionar = esResponsable || p.Acceso == NivelesAcceso.Cuidador;
            p.PuedeRegistrar = p.Acceso == NivelesAcceso.Cuidador || (p.ResponsableId == usuarioId && p.ResponsableCuida);
            p.Plan = responsables.GetValueOrDefault(p.ResponsableId)?.Plan ?? Planes.Gratis;
        }
    }
}
