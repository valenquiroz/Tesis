using Microsoft.AspNetCore.Identity;

namespace Cuida.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    // plan del grupo de este usuario (ver Models/Planes.cs); a quien cuida en el grupo
    // de otro lo cubre el plan de ese dueño
    public string Plan { get; set; } = Planes.Gratis;

    // cómo lo ven los cuidadores de su grupo, por ejemplo una residencia
    public string? NombreGrupo { get; set; }

    public ICollection<PersonaAsistida> PersonasAsistidas { get; set; }
        = new List<PersonaAsistida>();
}
