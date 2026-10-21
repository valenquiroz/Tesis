using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

// Diario de cuidado: lo que pasó en el día, para llevar a la consulta
public class EntradaDiario
{
    public int Id { get; set; }

    public int PersonaAsistidaId { get; set; }

    [JsonIgnore]
    public PersonaAsistida? PersonaAsistida { get; set; }

    [Required]
    public string AutorId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? Autor { get; set; }

    public DateTime Fecha { get; set; }

    [Required, MaxLength(30)]
    public string Tipo { get; set; } = string.Empty;

    // 1 (leve) a 5 (muy fuerte); opcional
    public int? Intensidad { get; set; }

    [Required, MaxLength(2000)]
    public string Texto { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public static class TiposDiario
{
    public static readonly string[] Todos = { "Crisis", "Síntoma", "Ánimo", "Sueño", "Alimentación", "Avance", "Otro" };
}

public class EntradaDiarioDto
{
    [Required(ErrorMessage = "Elegí la persona.")]
    public int PersonaAsistidaId { get; set; }

    [Required(ErrorMessage = "Indicá la fecha.")]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "Elegí el tipo de registro.")]
    public string Tipo { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "La intensidad va de 1 a 5.")]
    public int? Intensidad { get; set; }

    [Required(ErrorMessage = "Contá qué pasó.")]
    [StringLength(2000, MinimumLength = 3, ErrorMessage = "El texto debe tener entre 3 y 2000 caracteres.")]
    public string Texto { get; set; } = string.Empty;
}
