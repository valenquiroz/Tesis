using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

public class Turno
{
    public int Id { get; set; }

    [Required]
    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaHora { get; set; }

    public string? Lugar { get; set; }

    public string? Profesional { get; set; }

    public string? Especialidad { get; set; }

    public string? Observaciones { get; set; }

    // controles que se repiten (ej. audiometría cada 12 meses): se avisa cuando toca pedir el próximo
    public int? RepetirCadaMeses { get; set; }

    // los que crea el sistema (ej. aviso de vencimiento del CUD)
    [MaxLength(60)]
    public string? Origen { get; set; }

    [Required]
    public int PersonaAsistidaId { get; set; }

    public PersonaAsistida? PersonaAsistida { get; set; }

    [Required]
    public string CuidadorId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? Cuidador { get; set; }
}