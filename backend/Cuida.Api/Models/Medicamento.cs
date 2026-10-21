using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

public class Medicamento
{
    public int Id { get; set; }

    public int PersonaAsistidaId { get; set; }

    [JsonIgnore]
    public PersonaAsistida? PersonaAsistida { get; set; }

    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    // "1 comprimido", "5 ml"
    [MaxLength(80)]
    public string? Dosis { get; set; }

    // horas del día separadas por coma: "08:00,20:00"
    [Required, MaxLength(120)]
    public string Horarios { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Indicaciones { get; set; }

    [MaxLength(120)]
    public string? Medico { get; set; }

    public DateTime? VencimientoReceta { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<TomaMedicamento> Tomas { get; set; } = new List<TomaMedicamento>();
}

// Registro de una toma programada: si se dio o se salteó
public class TomaMedicamento
{
    public int Id { get; set; }

    public int MedicamentoId { get; set; }

    [JsonIgnore]
    public Medicamento? Medicamento { get; set; }

    public DateTime Fecha { get; set; }

    [Required, MaxLength(5)]
    public string Horario { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Estado { get; set; } = EstadosToma.Tomada;

    [Required]
    public string RegistradoPorId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? RegistradoPor { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}

public static class EstadosToma
{
    public const string Tomada = "Tomada";
    public const string Omitida = "Omitida";
}

public class MedicamentoDto
{
    [Required(ErrorMessage = "Elegí la persona.")]
    public int PersonaAsistidaId { get; set; }

    [Required(ErrorMessage = "Ingresá el nombre del medicamento.")]
    [StringLength(120, ErrorMessage = "El nombre es demasiado largo.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Dosis { get; set; }

    [Required(ErrorMessage = "Indicá al menos un horario.")]
    [StringLength(120)]
    public string Horarios { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Indicaciones { get; set; }

    [StringLength(120)]
    public string? Medico { get; set; }

    public DateTime? VencimientoReceta { get; set; }

    public bool Activo { get; set; } = true;
}

public class RegistrarTomaDto
{
    [Required]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "Indicá el horario.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "El horario debe tener el formato HH:mm.")]
    public string Horario { get; set; } = string.Empty;

    // null: deshace el registro
    public string? Estado { get; set; }
}
