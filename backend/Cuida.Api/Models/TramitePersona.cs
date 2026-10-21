using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

// En qué punto está cada trámite de la guía para una persona asistida.
// El contenido del trámite vive en Services/GuiaTramites.cs; acá solo el avance.
public class TramitePersona
{
    public int Id { get; set; }

    public int PersonaAsistidaId { get; set; }

    [JsonIgnore]
    public PersonaAsistida? PersonaAsistida { get; set; }

    [Required, MaxLength(60)]
    public string Codigo { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Estado { get; set; } = EstadosTramite.Pendiente;

    [MaxLength(500)]
    public string? Nota { get; set; }

    public DateTime FechaActualizacion { get; set; }
}

public static class EstadosTramite
{
    public const string Pendiente = "Pendiente";
    public const string EnCurso = "EnCurso";
    public const string Hecho = "Hecho";
    public const string NoCorresponde = "NoCorresponde";

    public static readonly string[] Todos = { Pendiente, EnCurso, Hecho, NoCorresponde };
}

public class ActualizarTramiteDto
{
    [Required(ErrorMessage = "Indicá el estado.")]
    public string Estado { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La nota no puede superar los 500 caracteres.")]
    public string? Nota { get; set; }
}
