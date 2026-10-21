using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

public class Documento
{
    public int Id { get; set; }

    [Required]
    public string NombreArchivo { get; set; } = string.Empty;

    [Required]
    public string NombreOriginal { get; set; } = string.Empty;

    public string? TipoContenido { get; set; }

    public long TamanoBytes { get; set; }

    [JsonIgnore]
    public string RutaArchivo { get; set; } = string.Empty;

    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;

    // si se subió desde un trámite de la guía
    [MaxLength(60)]
    public string? TramiteCodigo { get; set; }

    [Required]
    public int PersonaAsistidaId { get; set; }

    public PersonaAsistida? PersonaAsistida { get; set; }

    [Required]
    public string CuidadorId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? Cuidador { get; set; }
}