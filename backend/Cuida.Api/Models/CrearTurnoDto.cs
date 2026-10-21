using System.ComponentModel.DataAnnotations;

namespace Cuida.Api.Models;

public class CrearTurnoDto
{
    [Required(ErrorMessage = "Ingresá un título para el turno.")]
    [StringLength(150, ErrorMessage = "El título es demasiado largo.")]
    public string Titulo { get; set; } = string.Empty;

    // hora local, sin zona (viene del input datetime-local)
    [Required(ErrorMessage = "Indicá la fecha y hora del turno.")]
    public DateTime FechaHora { get; set; }

    [StringLength(150)]
    public string? Lugar { get; set; }

    [StringLength(150)]
    public string? Profesional { get; set; }

    [StringLength(100)]
    public string? Especialidad { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones son demasiado largas.")]
    public string? Observaciones { get; set; }

    [Range(1, 24, ErrorMessage = "El control se puede repetir cada 1 a 24 meses.")]
    public int? RepetirCadaMeses { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná una persona asistida.")]
    public int PersonaAsistidaId { get; set; }
}
