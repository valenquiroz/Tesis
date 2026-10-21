using Cuida.Api.Data;
using Cuida.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Services;

// El grupo de un dueño: sus personas y quienes las cuidan o miran.
public static class Grupos
{
    public static IQueryable<string> IntegrantesIds(this ApplicationDbContext context, string duenoId) =>
        context.PersonasCompartidas
            .Where(c => c.PersonaAsistida!.ResponsableId == duenoId)
            .Select(c => c.UsuarioId)
            .Distinct();

    public record PersonaEnGrupo(int Id, string Nombre, string Rol);

    // Un grupo de otro dueño en el que este usuario cuida o mira. Su plan lo paga ese dueño.
    public record GrupoAjeno(string DuenoId, string Nombre, string Dueno, string Plan, List<PersonaEnGrupo> Personas);

    public static async Task<List<GrupoAjeno>> GruposDondeAyudaAsync(this ApplicationDbContext context, string usuarioId)
    {
        var asignaciones = await context.PersonasCompartidas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .Select(c => new
            {
                c.PersonaAsistida!.ResponsableId,
                c.PersonaAsistida.Responsable!.NombreCompleto,
                c.PersonaAsistida.Responsable.NombreGrupo,
                c.PersonaAsistida.Responsable.Plan,
                c.PersonaAsistidaId,
                Persona = c.PersonaAsistida.NombreCompleto,
                c.Rol,
            })
            .ToListAsync();

        return asignaciones
            .GroupBy(a => a.ResponsableId)
            .Select(g => new GrupoAjeno(
                g.Key,
                g.First().NombreGrupo ?? g.First().NombreCompleto,
                g.First().NombreCompleto,
                Planes.De(g.First().Plan).Codigo,
                g.OrderBy(a => a.Persona).Select(a => new PersonaEnGrupo(a.PersonaAsistidaId, a.Persona, a.Rol)).ToList()))
            .OrderBy(g => g.Nombre)
            .ToList();
    }

    // null si se puede sumar a esa cuenta al grupo; si no, el motivo
    public static async Task<string?> MotivoParaNoSumarAsync(this ApplicationDbContext context, string duenoId, string invitadoId)
    {
        var limites = Planes.De(await context.Users.Where(u => u.Id == duenoId).Select(u => u.Plan).FirstOrDefaultAsync());

        if (!limites.Grupo)
            return $"Con el plan Gratis a la persona la cuidás vos. Para sumar cuidadores u observadores, pasate al plan {Planes.QueIncluye(l => l.Grupo)}.";

        if (limites.Integrantes is int maximo)
        {
            var integrantes = await context.IntegrantesIds(duenoId).ToListAsync();
            if (!integrantes.Contains(invitadoId) && integrantes.Count >= maximo)
                return $"Tu plan permite hasta {maximo} personas en el grupo. Para sumar más, pasate al plan {Planes.QueIncluye(l => l.Integrantes is null || l.Integrantes > maximo)}.";
        }

        return null;
    }
}
