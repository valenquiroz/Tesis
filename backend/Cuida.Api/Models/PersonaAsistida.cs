using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

public class PersonaAsistida
{
    public int Id { get; set; }

    [Required]
    public string NombreCompleto { get; set; } = string.Empty;

    public DateTime? FechaNacimiento { get; set; }

    public string? RelacionConCuidador { get; set; }

    public string? TipoDiscapacidad { get; set; }

    public string? ObraSocial { get; set; }

    public string? Localidad { get; set; }

    [MaxLength(200)]
    public string? Domicilio { get; set; }

    public string? Observaciones { get; set; }

    // quien la cargó y administra: la familia, o el gerente si es de una institución.
    // La columna se sigue llamando CuidadorId por compatibilidad con la base.
    [Required]
    [Column("CuidadorId")]
    public string ResponsableId { get; set; } = string.Empty;

    [JsonIgnore]
    public ApplicationUser? Responsable { get; set; }

    // false = el responsable solo mira lo que registran los cuidadores (modo observador)
    public bool ResponsableCuida { get; set; } = true;

    public string? Necesidades { get; set; }

    // Ficha de emergencia
    [MaxLength(10)]
    public string? GrupoSanguineo { get; set; }

    [MaxLength(300)]
    public string? Alergias { get; set; }

    [MaxLength(300)]
    public string? Diagnostico { get; set; }

    [MaxLength(60)]
    public string? NumeroAfiliado { get; set; }

    [MaxLength(120)]
    public string? MedicoCabecera { get; set; }

    [MaxLength(40)]
    public string? TelefonoMedico { get; set; }

    // cómo comunicarse, qué la calma, qué evitar
    [MaxLength(1000)]
    public string? InformacionEmergencia { get; set; }

    public DateTime? VencimientoCud { get; set; }

    // se completan al responder, según quién pregunta (ver AccesoPersonas)
    [NotMapped]
    public bool EsTitular { get; set; } = true;

    [NotMapped]
    public string? NombreTitular { get; set; }

    // Responsable, Cuidador u Observador
    [NotMapped]
    public string Acceso { get; set; } = NivelesAcceso.Responsable;

    [NotMapped]
    public bool PuedeAdministrar { get; set; }

    [NotMapped]
    public bool PuedeGestionar { get; set; }

    [NotMapped]
    public bool PuedeRegistrar { get; set; }

    // el plan que cubre a esta persona: el de su responsable (el dueño del grupo)
    [NotMapped]
    public string Plan { get; set; } = Planes.Gratis;

    // nombres de quienes la cuidan (sin observadores), para quien la administra
    [NotMapped]
    public List<string> Cuidadores { get; set; } = new();
}