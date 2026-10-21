using System.Text.Json.Serialization;

namespace Cuida.Api.Models;

// "Me sirvió" / "No me sirvió" sobre un recurso recomendado, por persona asistida.
// Lo usa el recomendador: la propia valoración reordena la lista y las de otros
// cuidadores con el mismo tipo de discapacidad suman o restan (parte colaborativa).
public class ValoracionRecurso
{
    public int Id { get; set; }

    public int PersonaAsistidaId { get; set; }

    [JsonIgnore]
    public PersonaAsistida? PersonaAsistida { get; set; }

    public int RecursoRecomendableId { get; set; }

    [JsonIgnore]
    public RecursoRecomendable? RecursoRecomendable { get; set; }

    public bool Util { get; set; }

    public DateTime Fecha { get; set; }
}

public class ValorarRecursoDto
{
    // null deshace la valoración
    public bool? Util { get; set; }
}
