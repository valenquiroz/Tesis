using System.ComponentModel.DataAnnotations;

namespace Cuida.Api.Models;

public class PersonaAsistidaRequest
{
    [Required(ErrorMessage = "Ingresá el nombre completo.")]
    [StringLength(150, ErrorMessage = "El nombre es demasiado largo.")]
    public string NombreCompleto { get; set; } = string.Empty;

    public DateTime? FechaNacimiento { get; set; }

    [StringLength(80)]
    public string? RelacionConCuidador { get; set; }

    [StringLength(80)]
    public string? TipoDiscapacidad { get; set; }

    [StringLength(80)]
    public string? ObraSocial { get; set; }

    [StringLength(100)]
    public string? Localidad { get; set; }

    [StringLength(200)]
    public string? Domicilio { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones son demasiado largas.")]
    public string? Observaciones { get; set; }

    // separadas por coma
    [StringLength(500)]
    public string? Necesidades { get; set; }

    [StringLength(10)]
    public string? GrupoSanguineo { get; set; }

    [StringLength(300, ErrorMessage = "Las alergias son demasiado largas.")]
    public string? Alergias { get; set; }

    [StringLength(300, ErrorMessage = "El diagnóstico es demasiado largo.")]
    public string? Diagnostico { get; set; }

    [StringLength(60)]
    public string? NumeroAfiliado { get; set; }

    [StringLength(120)]
    public string? MedicoCabecera { get; set; }

    [StringLength(40)]
    public string? TelefonoMedico { get; set; }

    [StringLength(1000, ErrorMessage = "La información de emergencia es demasiado larga.")]
    public string? InformacionEmergencia { get; set; }

    public DateTime? VencimientoCud { get; set; }
}
