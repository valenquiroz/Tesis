using System.ComponentModel.DataAnnotations;

namespace Cuida.Api.Models;

// El grupo de un dueño son las personas que cuida (las que cargó) y la gente que sumó
// para cuidarlas o mirarlas. No es una tabla: sale de PersonaAsistida.ResponsableId y
// de PersonaCompartida. Sirve igual para una familia que para una residencia.
public class SumarAlGrupoDto
{
    [Required(ErrorMessage = "Ingresá el correo de la persona.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Email { get; set; } = string.Empty;

    public string Rol { get; set; } = NivelesAcceso.Cuidador;

    // a quiénes va a cuidar o mirar
    public List<int> PersonasIds { get; set; } = new();
}

// a quiénes acompaña y con qué rol; reemplaza lo anterior
public class AsignacionDto
{
    public string Rol { get; set; } = NivelesAcceso.Cuidador;

    public List<int> PersonasIds { get; set; } = new();
}

public class NombreGrupoDto
{
    // por ejemplo "Residencia Las Glicinas"; vacío = se usa el nombre del dueño
    [MaxLength(150, ErrorMessage = "El nombre es demasiado largo.")]
    public string? NombreGrupo { get; set; }
}
