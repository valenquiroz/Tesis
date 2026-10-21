using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

// Autoevaluación de esfuerzo del cuidador. Es del cuidador, no de la persona asistida.
public class EvaluacionEsfuerzo
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // "1,0,1,..." en el orden de las preguntas
    [Required, MaxLength(60)]
    public string Respuestas { get; set; } = string.Empty;

    public int Puntaje { get; set; }
}

public class ResponderEvaluacionDto
{
    [Required(ErrorMessage = "Faltan las respuestas.")]
    public bool[] Respuestas { get; set; } = Array.Empty<bool>();
}
