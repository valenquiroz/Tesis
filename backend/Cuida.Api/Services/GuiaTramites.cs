using Cuida.Api.Models;

namespace Cuida.Api.Services;

// Hoja de ruta de trámites para una persona con discapacidad.
//
// Fuentes (revisadas en octubre de 2026):
// - Normativas de certificación por tipo de discapacidad (Ministerio de Salud):
//   Disp. SNR 1019/2015 (motora), 639/2015 (visual), 82/2015 (auditiva),
//   648/2015 (intelectual y mental) y 500/2015 (visceral). Cada una trae en su
//   sección "Requisitos" lo que hay que llevar a la junta evaluadora.
// - argentina.gob.ar: cómo obtener el CUD, Símbolo Internacional de Acceso,
//   transporte gratuito (CNRT), asignación por hijo con discapacidad.
// - Superintendencia de Servicios de Salud: cobertura al 100% (Ley 24.901).
// - ANSES: pensión no contributiva por invalidez laboral.
// Desde enero de 2026 (DNU 942/2025) la ANDIS pasó a ser la Secretaría Nacional
// de Discapacidad, dentro del Ministerio de Salud.
public static class GuiaTramites
{
    public const string FechaRevision = "octubre de 2026";

    public static readonly EtapaGuia[] Etapas =
    {
        new(1, "Antes de empezar", "Averiguá qué te van a pedir y dónde."),
        new(2, "Reunir los estudios", "Lo que pide la junta según el tipo de discapacidad."),
        new(3, "La junta evaluadora", "El turno, la evaluación y el certificado."),
        new(4, "Con el CUD en la mano", "Derechos que se habilitan con el certificado."),
    };

    private const string Junta = "Junta evaluadora de tu domicilio";
    private const string Secretaria = "Secretaría Nacional de Discapacidad";

    private static readonly string[] Mental = { "Intelectual", "Psicosocial" };

    public static readonly PasoGuia[] Pasos =
    {
        // ---------------------------------------------------------------
        // 1. Antes de empezar
        // ---------------------------------------------------------------
        new()
        {
            Codigo = "cud-consulta",
            Etapa = 1,
            Titulo = "Hacer la consulta de requisitos del CUD",
            Resumen = "El Certificado Único de Discapacidad es la puerta de entrada a casi todo lo demás. Es gratuito y voluntario. " +
                      "La consulta oficial te dice qué llevar y a qué junta pedir turno según la edad, la localidad y la condición de salud.",
            Pasos = new[]
            {
                "Entrá a la consulta de requisitos en argentina.gob.ar.",
                "Respondé las preguntas sobre edad, localidad y problema de salud.",
                "Guardá el listado que te devuelve y el lugar donde pedir el turno.",
            },
            Organismo = Secretaria,
            Enlace = "https://www.argentina.gob.ar/cud/consulta-de-requisitos-para-tramitar-el-cud",
            Fuente = "argentina.gob.ar · Consulta de requisitos para tramitar el CUD",
            Imprescindible = true,
        },
        new()
        {
            Codigo = "cud-planilla",
            Etapa = 1,
            Titulo = "Completar la planilla de solicitud y juntar el DNI",
            Resumen = "Además de los estudios, todas las juntas piden la identidad de la persona y la planilla de solicitud.",
            Documentacion = new[]
            {
                new LineaDoc("DNI original y legible de la persona, más una fotocopia."),
                new LineaDoc("Planilla de solicitud del CUD (se completa en línea)."),
                new LineaDoc("Si es una renovación: el certificado anterior original."),
                new LineaDoc("Si tiene obra social: fotocopias del carnet y del DNI (algunas juntas también piden el último recibo de sueldo)."),
            },
            Organismo = Secretaria,
            Enlace = "https://www.argentina.gob.ar/cud/como-obtener-el-certificado-unico-de-discapacidad-cud",
            Fuente = "argentina.gob.ar · Formulario en línea de la planilla de solicitud del CUD",
            Imprescindible = true,
        },

        // ---------------------------------------------------------------
        // 2. Estudios según el tipo de discapacidad
        // ---------------------------------------------------------------
        new()
        {
            Codigo = "estudios-motora",
            Etapa = 2,
            Titulo = "Estudios para discapacidad motora",
            Resumen = "Incluye causas neurológicas, traumatológicas y reumatológicas. La persona tiene que ir a la evaluación.",
            Documentacion = new[]
            {
                new LineaDoc("Certificado médico original, de no más de 6 meses, del especialista o médico tratante: diagnóstico completo, estado actual, tratamientos hechos y cómo respondió."),
                new LineaDoc("Estudios complementarios según el caso: electromiograma, radiografías, ecografías, tomografía o resonancia, con informe."),
                new LineaDoc("Planilla específica completada por el especialista si corresponde: patología de columna, artritis reumatoidea, miastenia gravis o esclerosis múltiple."),
                new LineaDoc("Planilla de pautas madurativas firmada por el médico tratante (en niños con retraso madurativo).", 4),
            },
            Aviso = "No se evalúa durante brotes o reagudizaciones: conviene sacar turno en un período estable.",
            Organismo = Junta,
            Enlace = "https://www.argentina.gob.ar/sites/default/files/2018/02/norma_para_la_evaluacion_de_personas_con_discapacidad_con_deficiencia_fisica_de_origen_motor_1_422026.pdf",
            Fuente = "Disposición SNR 1019/2015 · Normativa de certificación motora",
            Imprescindible = true,
            Tipos = new[] { "Motora" },
        },
        new()
        {
            Codigo = "estudios-visual",
            Etapa = 2,
            Titulo = "Estudios para discapacidad visual",
            Resumen = "El certificado lo completa un oftalmólogo. La junta mira sobre todo la agudeza visual y el campo visual del mejor ojo.",
            Documentacion = new[]
            {
                new LineaDoc("Certificado del oftalmólogo con letra clara: agudeza visual con y sin corrección de ambos ojos, refracción, tensión ocular, biomicroscopía y fondo de ojo."),
                new LineaDoc("Campo visual de los 30° centrales de ambos ojos, informado por el especialista."),
                new LineaDoc("Otros estudios que avalen el diagnóstico (OCT, ecografía, etc.), tratamientos y diagnóstico de la causa."),
                new LineaDoc("En niños de 0 a 3 años: fijación y seguimiento, mirada preferencial, motilidad ocular, refracción, fondo de ojo, potenciales visuales evocados, estudio neurológico y ecografía ocular.", 3),
                new LineaDoc("En niños de 3 a 5 años: agudeza visual con cartillas infantiles, biomicroscopía, motilidad, refracción, fondo de ojo y ecografía ocular. Se recomienda un oftalmólogo pediatra.", 5),
            },
            Organismo = Junta,
            Enlace = "https://www.argentina.gob.ar/sites/default/files/2018/02/normativa_para_la_certificacion_de_personas_con_discapacidad_con_deficiencia_sensorial_de_origen_visual_1_422026.pdf",
            Fuente = "Disposición SNR 639/2015 · Normativa de certificación visual",
            Imprescindible = true,
            Tipos = new[] { "Visual" },
        },
        new()
        {
            Codigo = "estudios-auditiva",
            Etapa = 2,
            Titulo = "Estudios para discapacidad auditiva",
            Resumen = "Los estudios tienen que ser de los últimos 6 meses. Se evalúa sin audífono ni implante.",
            Documentacion = new[]
            {
                new LineaDoc("Resumen de historia clínica o certificado del especialista (últimos 6 meses): diagnóstico con tipo y grado de hipoacusia, estado actual, evolución, tratamiento y complicaciones."),
                new LineaDoc("Audiometría tonal de los últimos 6 meses. Si usa audífono o implante, se hace con el equipo desconectado."),
                new LineaDoc("Logoaudiometría, cuando se pueda hacer."),
                new LineaDoc("En menores de 3 años: estudios electrofisiológicos (potenciales evocados PEAT/BERA y otoemisiones acústicas).", 3),
                new LineaDoc("En niños con trastornos del lenguaje: informes escolar, fonoaudiológico, psicopedagógico o neurolingüístico.", 12),
            },
            Organismo = Junta,
            Enlace = "https://www.argentina.gob.ar/sites/default/files/2018/02/normativa_para_la_certificacion_de_personas_con_discapacidad_auditiva_1_422026.pdf",
            Fuente = "Disposición SNR 82/2015 · Normativa de certificación auditiva",
            Imprescindible = true,
            Tipos = new[] { "Auditiva" },
        },
        new()
        {
            Codigo = "estudios-mental",
            Etapa = 2,
            Titulo = "Estudios para discapacidad intelectual o psicosocial",
            Resumen = "La misma normativa cubre la discapacidad intelectual y la mental (psicosocial). Las planillas tienen valor de certificado médico.",
            Documentacion = new[]
            {
                new LineaDoc("Planilla de evaluación de trastornos mentales y del comportamiento, completa y firmada por el médico o equipo tratante, de no más de 6 meses."),
                new LineaDoc("Certificado o resumen de historia clínica original con diagnóstico (CIE-10), secuelas, estado actual, antecedentes, tiempo de evolución, internaciones y tratamientos."),
                new LineaDoc("Informes complementarios según el caso: evaluación neurocognitiva, coeficiente intelectual, informe psicológico, informe escolar o psicopedagógico, neuroimágenes."),
                new LineaDoc("Si hay epilepsia: planilla de epilepsia y, si tomó dos drogas sin control de las crisis, dosaje en sangre (no más de 6 meses)."),
            },
            Aviso = "En la discapacidad psicosocial se certifican las secuelas después de al menos 6 meses de diagnóstico y tratamiento.",
            Organismo = Junta,
            Enlace = "https://www.argentina.gob.ar/sites/default/files/2018/02/normativa_para_la_certificacion_de_personas_con_discapacidad_con_deficiencia_intelectual_y_mental_1_422026.pdf",
            Fuente = "Disposición SNR 648/2015 · Normativa de certificación intelectual y mental",
            Imprescindible = true,
            Tipos = Mental,
        },
        new()
        {
            Codigo = "estudios-visceral",
            Etapa = 2,
            Titulo = "Estudios para discapacidad visceral",
            Resumen = "Abarca condiciones cardíacas, renales, respiratorias, hepáticas y digestivas. Los originales quedan retenidos: no se aceptan fotocopias.",
            Documentacion = new[]
            {
                new LineaDoc("Certificado original con diagnóstico completo, estado actual, secuelas y tratamiento médico y quirúrgico, con fecha, firma y sello (no más de 6 meses)."),
                new LineaDoc("Si es cardiológico o renal: la planilla específica completada por el especialista."),
                new LineaDoc("Estudios del órgano afectado: ecocardiograma (corazón), filtrado glomerular y laboratorio (riñón), espirometría (pulmón), laboratorio hepático (hígado), laboratorio y endoscopía (digestivo)."),
                new LineaDoc("Asma crónica: más de dos espirometrías separadas por al menos 2 meses."),
            },
            Organismo = Junta,
            Enlace = "https://www.argentina.gob.ar/sites/default/files/2018/02/normativa_para_la_certificacion_de_personas_con_discapacidad_con_deficiencia_fisica_de_origen_visceral.doc_1_422026.pdf",
            Fuente = "Disposición SNR 500/2015 · Normativa de certificación visceral",
            Imprescindible = true,
            Tipos = new[] { "Visceral" },
        },
        new()
        {
            Codigo = "estudios-multiple",
            Etapa = 2,
            Titulo = "Estudios para cada discapacidad",
            Resumen = "Cuando hay más de una deficiencia, la junta evalúa cada una con su normativa. Llevá los estudios de todas.",
            Documentacion = new[]
            {
                new LineaDoc("Un certificado médico de no más de 6 meses con todos los diagnósticos, el estado actual y los tratamientos."),
                new LineaDoc("Los estudios propios de cada deficiencia (por ejemplo: audiometría si hay hipoacusia, campo visual si hay baja visión, imágenes si hay compromiso motor)."),
                new LineaDoc("Las planillas específicas que correspondan a cada una."),
            },
            Aviso = "Revisá el resultado de la consulta del primer paso: indica qué estudios pide tu junta.",
            Organismo = Junta,
            Enlace = "https://www.argentina.gob.ar/servicio/como-obtener-el-certificado-unico-de-discapacidad-cud",
            Fuente = "argentina.gob.ar · Normativas de certificación por tipo de discapacidad",
            Imprescindible = true,
            Tipos = new[] { "Múltiple" },
        },

        // ---------------------------------------------------------------
        // 3. Junta evaluadora
        // ---------------------------------------------------------------
        new()
        {
            Codigo = "cud-junta",
            Etapa = 3,
            Titulo = "Pedir turno e ir a la junta evaluadora",
            Resumen = "Una junta interdisciplinaria evalúa a la persona. No alcanza con el diagnóstico: miran cómo afecta su vida diaria.",
            Pasos = new[]
            {
                "Con la documentación lista, pedí turno en el lugar que te indicó la consulta.",
                "Anotá la fecha y concurrí con la persona: tiene que estar presente.",
                "El certificado (o la denegatoria) lo retira cualquier persona mayor de 18 años con el DNI original de la persona, en la fecha que te den.",
                "Una vez emitido, el CUD digital aparece en Mi Argentina y tiene la misma validez que el de papel.",
            },
            Aviso = "Si te rechazan el certificado, pedí la denegatoria por escrito: sirve para reclamar o volver a presentarte.",
            Organismo = Junta,
            Enlace = "https://www.argentina.gob.ar/servicio/como-obtener-el-certificado-unico-de-discapacidad-cud",
            Fuente = "argentina.gob.ar · Cómo obtener el Certificado Único de Discapacidad",
            Imprescindible = true,
        },

        // ---------------------------------------------------------------
        // 4. Con el CUD
        // ---------------------------------------------------------------
        new()
        {
            Codigo = "cobertura-100",
            Etapa = 4,
            Titulo = "Presentar el CUD en la obra social",
            Resumen = "Con el certificado, la obra social o prepaga tiene que cubrir al 100% las prestaciones vinculadas a la discapacidad (Ley 24.901): " +
                      "rehabilitación, medicamentos de la patología, prótesis, órtesis y audífonos.",
            Pasos = new[]
            {
                "Llevá una copia del CUD a la obra social y pedí que lo registren.",
                "Pedí las prestaciones con la orden del médico tratante.",
                "Si no tiene obra social, las prestaciones las cubre el Estado (por ejemplo, a través del programa Incluir Salud).",
            },
            Organismo = "Obra social / Superintendencia de Servicios de Salud",
            Enlace = "https://www.argentina.gob.ar/sssalud/usuarios/cobertura-prestacional/discapacidad",
            Fuente = "Superintendencia de Servicios de Salud · Cobertura por discapacidad",
            Imprescindible = true,
        },
        new()
        {
            Codigo = "transporte-gratuito",
            Etapa = 4,
            Titulo = "Viajar gratis en transporte público",
            Resumen = "El CUD permite viajar sin cargo en colectivos y trenes de jurisdicción nacional y en micros de larga distancia. " +
                      "Si el certificado lo indica, también viaja gratis un acompañante.",
            Pasos = new[]
            {
                "Llevá siempre el DNI y el CUD (original o digital en Mi Argentina).",
                "Para micros de larga distancia, reservá el pasaje al menos 48 horas antes en el sistema web de la CNRT.",
                "Vincular el CUD a la SUBE es opcional.",
                "Los colectivos provinciales tienen su propio régimen: consultá en tu provincia.",
            },
            Organismo = "CNRT",
            Enlace = "https://www.argentina.gob.ar/transporte/cnrt/derechos-de-los-usuarios/derechos-de-las-personas-con-discapacidad",
            Fuente = "CNRT · Derechos de las personas con discapacidad",
            Imprescindible = false,
        },
        new()
        {
            Codigo = "simbolo-acceso",
            Etapa = 4,
            Titulo = "Sacar el Símbolo Internacional de Acceso",
            Resumen = "Permite estacionar y circular con el auto que traslada a la persona. Se tramita en Mi Argentina y es gratuito; no hace falta ser dueño del auto.",
            Pasos = new[]
            {
                "Validá tu identidad en Mi Argentina.",
                "Cargá los datos del CUD y la patente del vehículo.",
                "Si el CUD es anterior a 2015 (sin código de barras), primero hay que registrarlo por TAD.",
            },
            Organismo = Secretaria,
            Enlace = "https://www.argentina.gob.ar/obtener-simbolo-internacional-de-acceso-digital",
            Fuente = "argentina.gob.ar · Obtener digitalmente el Símbolo Internacional de Acceso",
            Imprescindible = false,
        },
        new()
        {
            Codigo = "asignacion-hijo",
            Etapa = 4,
            Titulo = "Pedir la Asignación por Hijo con Discapacidad",
            Resumen = "La ANSES la paga sin límite de edad y sin tope de ingresos. Antes hay que tramitar la autorización con el CUD.",
            Pasos = new[]
            {
                "Pedí turno en ANSES (o entrá a Mi ANSES) para tramitar la autorización por hijo con discapacidad.",
                "Presentá el CUD vigente.",
                "Se cobra a través de uno solo de los progenitores.",
            },
            Organismo = "ANSES",
            Enlace = "https://www.argentina.gob.ar/servicio/tramitar-asignacion-familiar-por-hijo-o-por-hijo-con-discapacidad",
            Fuente = "argentina.gob.ar · Tramitar asignación familiar por hijo con discapacidad",
            Imprescindible = false,
            Aplica = (persona, _) => (persona.RelacionConCuidador ?? "").Trim().StartsWith("hij", StringComparison.OrdinalIgnoreCase),
        },
        new()
        {
            Codigo = "pension-invalidez",
            Etapa = 4,
            Titulo = "Evaluar la Pensión No Contributiva por Invalidez",
            Resumen = "Es para quien tiene una disminución de la capacidad laboral del 66% o más y no cuenta con otros ingresos. " +
                      "No alcanza con el CUD: se acredita con un Certificado Médico Oficial y la evalúa un tribunal médico.",
            Documentacion = new[]
            {
                new LineaDoc("DNI de la persona."),
                new LineaDoc("Certificado Médico Oficial (CMO) emitido por un establecimiento público de salud, con los estudios que lo respalden."),
                new LineaDoc("No cobrar jubilación, retiro ni otra pensión, y no tener trabajo registrado (sí se permite el monotributo social)."),
            },
            Organismo = "ANSES",
            Enlace = "https://www.argentina.gob.ar/servicio/solicitar-la-pension-no-contributiva-por-invalidez-laboral",
            Fuente = "ANSES · Pensión No Contributiva por Invalidez Laboral",
            Imprescindible = false,
        },
        new()
        {
            Codigo = "cud-vencimiento",
            Etapa = 4,
            Titulo = "Anotar el vencimiento del CUD",
            Resumen = "Algunos certificados ya no vencen (por ejemplo, tras renovaciones previas o con equipamiento permanente). " +
                      "Si el tuyo tiene fecha, agendala con tiempo: para renovarlo hay que volver a la junta con el certificado vencido.",
            Pasos = new[]
            {
                "Mirá la fecha de vencimiento en el CUD o en Mi Argentina.",
                "Agendá un recordatorio unos meses antes en el calendario.",
            },
            Organismo = Secretaria,
            Enlace = "https://www.argentina.gob.ar/noticias/la-andis-establece-criterios-para-el-cud-sin-vencimiento",
            Fuente = "Resolución 1654/2024 · Criterios para el CUD sin vencimiento",
            Imprescindible = false,
        },
    };

    public static int? Edad(PersonaAsistida persona, DateTime hoy)
    {
        if (persona.FechaNacimiento is not DateTime nacimiento) return null;
        var edad = hoy.Year - nacimiento.Year;
        if (nacimiento.Date > hoy.AddYears(-edad)) edad--;
        return edad >= 0 ? edad : null;
    }

    // Pasos que corresponden a la persona, con la documentación filtrada por edad
    public static IEnumerable<PasoGuia> ParaPersona(PersonaAsistida persona, DateTime hoy)
    {
        var tipo = persona.TipoDiscapacidad?.Trim();
        var edad = Edad(persona, hoy);

        foreach (var paso in Pasos)
        {
            if (paso.Tipos != null &&
                (tipo == null || !paso.Tipos.Contains(tipo, StringComparer.OrdinalIgnoreCase)))
                continue;

            if (paso.Aplica != null && !paso.Aplica(persona, edad))
                continue;

            yield return paso with
            {
                Documentacion = paso.Documentacion
                    .Where(d => d.HastaEdad == null || edad == null || edad <= d.HastaEdad)
                    .ToArray()
            };
        }
    }
}

public record EtapaGuia(int Numero, string Titulo, string Descripcion);

// HastaEdad: la línea solo se muestra hasta esa edad (si no se conoce la edad, se muestra)
public record LineaDoc(string Texto, int? HastaEdad = null);

public record PasoGuia
{
    public string Codigo { get; init; } = string.Empty;
    public int Etapa { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string Resumen { get; init; } = string.Empty;
    public string[] Pasos { get; init; } = Array.Empty<string>();
    public LineaDoc[] Documentacion { get; init; } = Array.Empty<LineaDoc>();
    public string? Aviso { get; init; }
    public string Organismo { get; init; } = string.Empty;
    public string Enlace { get; init; } = string.Empty;
    public string Fuente { get; init; } = string.Empty;

    // false: "si corresponde"
    public bool Imprescindible { get; init; }

    // null: aplica a cualquier tipo de discapacidad
    public string[]? Tipos { get; init; }

    public Func<PersonaAsistida, int?, bool>? Aplica { get; init; }
}
