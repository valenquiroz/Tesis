using System.ComponentModel.DataAnnotations;

namespace Cuida.Api.Models;

public class RecursoRecomendable
{
    public int Id { get; set; }

    [Required]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    public string Categoria { get; set; } = string.Empty;

    public string? TipoDiscapacidad { get; set; }

    public string? ObraSocial { get; set; }

    public string? Localidad { get; set; }

    public string? Enlace { get; set; }

    public bool Activo { get; set; } = true;

    public string? Necesidades { get; set; }
}