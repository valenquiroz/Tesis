namespace Cuida.Api.Services;

// Índice de Esfuerzo del Cuidador (Robinson, 1983).
// Versión validada en población española por López Alonso y Moral Serrano (2005):
// 13 preguntas de sí o no, una respuesta afirmativa suma 1 punto
// y 7 puntos o más indican un nivel elevado de esfuerzo.
// Las preguntas están redactadas en rioplatense sin cambiar lo que mide cada una.
// Se eligió en lugar de la escala de Zarit porque esa tiene licencia de uso comercial.
public static class EscalaEsfuerzo
{
    public const int PuntoDeCorte = 7;

    public const string Referencia =
        "Índice de Esfuerzo del Cuidador (Robinson, 1983), validado en español por López Alonso y Moral Serrano (2005).";

    public static readonly PreguntaEsfuerzo[] Preguntas =
    {
        new("sueno", "¿Se te altera el sueño?", "Por ejemplo, porque te levantás o te despertás de noche para atenderla."),
        new("tiempo", "¿Te resulta un inconveniente?", "Por ejemplo, porque ayudarla te lleva mucho tiempo."),
        new("fisico", "¿Te representa un esfuerzo físico?", "Por ejemplo, sentarla, levantarla de una silla o bañarla."),
        new("restriccion", "¿Te limita?", "Por ejemplo, te deja menos tiempo libre o no podés salir ni visitar a nadie."),
        new("familia", "¿Hubo cambios en tu familia?", "Por ejemplo, se rompió la rutina o se perdió intimidad."),
        new("planes", "¿Tuviste que cambiar planes personales?", "Por ejemplo, rechazar un trabajo o no poder irte de vacaciones."),
        new("exigencias", "¿Tenés otras exigencias sobre tu tiempo?", "Por ejemplo, de otros miembros de la familia."),
        new("emocional", "¿Hubo cambios emocionales?", "Por ejemplo, discusiones fuertes."),
        new("conductas", "¿Algunas conductas te resultan molestas?", "Por ejemplo, la incontinencia, que le cueste recordar o que acuse a otros."),
        new("cambio", "¿Te duele ver cuánto cambió respecto de antes?", "Por ejemplo, sentir que es una persona distinta."),
        new("trabajo", "¿Hubo cambios en tu trabajo?", "Por ejemplo, por tener que reservar tiempo para cuidarla."),
        new("economico", "¿Es una carga económica?", null),
        new("desborde", "¿Te sentiste completamente desbordado o desbordada?", "Por ejemplo, por la preocupación por ella o por cómo seguir con el tratamiento."),
    };

    // Sugerencias según las áreas que salieron afirmativas
    private static readonly Dictionary<string, Sugerencia> Sugerencias = new()
    {
        ["sueno"] = new("Descanso", "Si las noches son difíciles, hablalo con el médico tratante: a veces se pueden ajustar horarios de medicación o sumar ayuda nocturna. Turnarse con otra persona aunque sea una noche por semana ya alivia.", null),
        ["fisico"] = new("Esfuerzo físico", "Una terapista ocupacional puede enseñarte técnicas de traslado y recomendar elementos de apoyo (grúas, sillas de baño, barandas) que la obra social cubre con el CUD.", "tramites"),
        ["restriccion"] = new("Tiempo propio", "Compartí el cuidado: podés sumar a un familiar o a un cuidador a tu grupo para que registre con vos, cada uno con su cuenta.", "grupo"),
        ["planes"] = new("Tiempo propio", "Compartí el cuidado: podés sumar a un familiar o a un cuidador a tu grupo para que registre con vos, cada uno con su cuenta.", "grupo"),
        ["economico"] = new("Lo económico", "Revisá los derechos que se habilitan con el CUD: cobertura al 100%, transporte gratuito, asignaciones y pensiones.", "tramites"),
        ["emocional"] = new("Lo emocional", "Contarlo ayuda. La línea nacional de salud mental (0800-999-0091) es gratuita, atiende las 24 horas y también a familiares.", null),
        ["desborde"] = new("Lo emocional", "Contarlo ayuda. La línea nacional de salud mental (0800-999-0091) es gratuita, atiende las 24 horas y también a familiares.", null),
        ["conductas"] = new("Conductas difíciles", "Anotá en el diario cuándo aparecen y qué las precede: con eso el equipo tratante puede orientarte mejor.", "diario"),
    };

    public static ResultadoEsfuerzo Evaluar(bool[] respuestas)
    {
        var puntaje = respuestas.Count(r => r);
        var elevado = puntaje >= PuntoDeCorte;

        var areas = Preguntas
            .Select((p, i) => (p.Codigo, Si: respuestas[i]))
            .Where(x => x.Si && Sugerencias.ContainsKey(x.Codigo))
            .Select(x => Sugerencias[x.Codigo])
            .DistinctBy(s => s.Titulo)
            .ToList();

        return new ResultadoEsfuerzo(
            puntaje,
            Preguntas.Length,
            elevado,
            elevado ? "Nivel elevado de esfuerzo" : "Sin nivel elevado de esfuerzo",
            elevado
                ? "Tus respuestas muestran que el cuidado te está exigiendo mucho. No es una falla tuya: es una señal para pedir ayuda y repartir la carga."
                : "Hoy el esfuerzo no aparece en un nivel elevado. Igual vale la pena repetir el test cada tanto: la carga cambia con el tiempo.",
            areas);
    }
}

public record PreguntaEsfuerzo(string Codigo, string Texto, string? Ejemplo);

// Seccion: vista del panel a la que lleva la sugerencia (null si no lleva a ninguna)
public record Sugerencia(string Titulo, string Texto, string? Seccion);

public record ResultadoEsfuerzo(
    int Puntaje,
    int Maximo,
    bool Elevado,
    string Nivel,
    string Mensaje,
    List<Sugerencia> Sugerencias);
