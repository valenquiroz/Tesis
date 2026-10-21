using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

// Integrante del equipo de cuidado de una persona, además de su responsable.
// - Cuidador: alguien contratado o un familiar que registra tomas, diario, turnos.
// - Observador: alguien de la familia que solo mira lo que se registra.
// El responsable (PersonaAsistida.ResponsableId) es el único que arma el equipo.
public class PersonaCompartida
{
    public int PersonaAsistidaId { get; set; }

    [JsonIgnore]
    public PersonaAsistida? PersonaAsistida { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? Usuario { get; set; }

    [Required, MaxLength(20)]
    public string Rol { get; set; } = NivelesAcceso.Cuidador;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
}

// Qué puede hacer cada uno con una persona:
// - Responsable: ve todo y administra (perfil, ficha, equipo). Registra tomas y diario
//   solo si también la cuida (PersonaAsistida.ResponsableCuida).
// - Cuidador: ve todo, registra y gestiona turnos, medicación, documentos y contactos.
// - Observador: ve todo y no cambia nada.
public static class NivelesAcceso
{
    public const string Responsable = "Responsable";
    public const string Cuidador = "Cuidador";
    public const string Observador = "Observador";

    public static readonly string[] DelEquipo = { Cuidador, Observador };
}

public class SumarAlEquipoDto
{
    [Required(ErrorMessage = "Ingresá el correo de la otra persona.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Email { get; set; } = string.Empty;

    public string Rol { get; set; } = NivelesAcceso.Cuidador;
}

public class CambiarRolDto
{
    public string Rol { get; set; } = string.Empty;
}

public class ResponsableCuidaDto
{
    public bool ResponsableCuida { get; set; }
}
