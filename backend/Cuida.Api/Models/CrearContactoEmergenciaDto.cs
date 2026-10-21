using System.ComponentModel.DataAnnotations;

namespace Cuida.Api.Models;

public class CrearContactoEmergenciaDto
{
    [Required(ErrorMessage = "Ingresá el nombre del contacto.")]
    [StringLength(150, ErrorMessage = "El nombre es demasiado largo.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Relacion { get; set; }

    [Required(ErrorMessage = "Ingresá un teléfono.")]
    [RegularExpression(@"^[0-9+()\-\s]{6,25}$",
        ErrorMessage = "El teléfono solo puede tener números, espacios, guiones, paréntesis y +.")]
    public string Telefono { get; set; } = string.Empty;

    [StringLength(40)]
    public string? TipoContacto { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná una persona asistida.")]
    public int PersonaAsistidaId { get; set; }
}
