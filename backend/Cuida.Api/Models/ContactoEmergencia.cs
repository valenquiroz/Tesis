using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

public class ContactoEmergencia
{
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Relacion { get; set; }

    [Required]
    public string Telefono { get; set; } = string.Empty;

    public string? TipoContacto { get; set; }

    [Required]
    public int PersonaAsistidaId { get; set; }

    public PersonaAsistida? PersonaAsistida { get; set; }

    [Required]
    public string CuidadorId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? Cuidador { get; set; }
}