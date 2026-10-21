using System.ComponentModel.DataAnnotations;

namespace Cuida.Api.Models;

// Un escrito ya publicado por especialistas (organismos, sociedades científicas,
// universidades). La plataforma no aloja el texto: muestra de qué trata y enlaza a la fuente.
// Los carga el administrador.
public class ArticuloRecomendado
{
    public int Id { get; set; }

    [Required, MaxLength(250)]
    public string Titulo { get; set; } = string.Empty;

    // personas o equipo que lo escribieron
    [Required, MaxLength(250)]
    public string Autores { get; set; } = string.Empty;

    // dónde se publicó: revista, organismo, sitio
    [Required, MaxLength(200)]
    public string Fuente { get; set; } = string.Empty;

    public int? Anio { get; set; }

    // de qué trata, escrito por la plataforma (no se copia el texto original)
    [Required, MaxLength(600)]
    public string Resumen { get; set; } = string.Empty;

    // null = sirve para cualquier tipo de discapacidad
    [MaxLength(40)]
    public string? TipoDiscapacidad { get; set; }

    [Required, MaxLength(500)]
    public string Enlace { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
}

public class ArticuloRecomendadoDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [MaxLength(250)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indicá quién lo escribió.")]
    [MaxLength(250)]
    public string Autores { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indicá dónde se publicó.")]
    [MaxLength(200)]
    public string Fuente { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "El año no es válido.")]
    public int? Anio { get; set; }

    [Required(ErrorMessage = "Escribí de qué trata.")]
    [MaxLength(600, ErrorMessage = "El resumen no puede superar los 600 caracteres.")]
    public string Resumen { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? TipoDiscapacidad { get; set; }

    [Required(ErrorMessage = "El enlace es obligatorio.")]
    [Url(ErrorMessage = "El enlace no es válido.")]
    [MaxLength(500)]
    public string Enlace { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}
