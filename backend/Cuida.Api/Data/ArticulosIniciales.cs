using Cuida.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Data;

// Escritos de especialistas ya publicados, para la sección Artículos.
// Fuentes de acceso libre (SciELO, Ministerio de Capital Humano, OMS/OPS, Consejo Argentino
// de Oftalmología), enlaces revisados en octubre de 2026. Los generales son sobre el cuidado;
// los demás, de uno o más tipos de discapacidad (separados por coma). Los resúmenes son propios:
// no se copia el texto original, se cuenta de qué trata y se enlaza a la fuente.
public static class ArticulosIniciales
{
    private const string MaterialesCuidadores =
        "Registro Nacional de Cuidadores Domiciliarios, Ministerio de Capital Humano";

    private static List<ArticuloRecomendado> Lista() => new()
    {
        // para cualquier persona que cuida
        new()
        {
            Titulo = "Los cuidadores domiciliarios y el entorno familiar",
            Autores = "Luciana Machluk",
            Fuente = MaterialesCuidadores,
            Resumen = "Material para cuidadores domiciliarios sobre la relación entre quien cuida en la casa y la familia de la persona cuidada.",
            Enlace = "https://www.argentina.gob.ar/sites/default/files/entorno_familiar.pdf",
        },
        new()
        {
            Titulo = "Los límites de los cuidados en domicilio",
            Autores = "Sonia Irene Hanine",
            Fuente = MaterialesCuidadores,
            Resumen = "Material para cuidadores domiciliarios sobre los límites del cuidado que se puede brindar en la casa.",
            Enlace = "https://www.argentina.gob.ar/sites/default/files/los_limites_de_los_cuidados_en_domicilio.pdf",
        },
        new()
        {
            Titulo = "La red de apoyo social en la vejez y el vínculo con el cuidador domiciliario",
            Autores = "Corina Soliverez y Claudia Arias",
            Fuente = MaterialesCuidadores,
            Resumen = "Material para cuidadores domiciliarios sobre la red de apoyo social de las personas mayores y su vínculo con quien las cuida.",
            Enlace = "https://www.argentina.gob.ar/sites/default/files/la_red_de_apoyo_social.pdf",
        },
        new()
        {
            Titulo = "Las caídas en las personas mayores y cómo prevenirlas",
            Autores = "Gustavo Alberto Spampinato",
            Fuente = MaterialesCuidadores,
            Resumen = "Material para cuidadores domiciliarios sobre las caídas en las personas mayores y cómo prevenirlas.",
            Enlace = "https://www.argentina.gob.ar/sites/default/files/caidas_en_personas_mayores.pdf",
        },
        new()
        {
            Titulo = "Derechos de los pacientes con enfermedades que se asocien con discapacidad: marco legal actual",
            Autores = "Graciela Moya",
            Fuente = "Archivos Argentinos de Pediatría, vol. 114, n.º 4",
            Anio = 2016,
            Resumen = "Revisión de las leyes argentinas que protegen a las personas con discapacidad, pensada para que el equipo de salud ayude a las familias a conocer y reclamar sus derechos.",
            Enlace = "https://www.scielo.org.ar/scielo.php?pid=S0325-00752016000400014&script=sci_arttext",
        },
        new()
        {
            Titulo = "iSupport para la demencia",
            Autores = "Organización Mundial de la Salud",
            Fuente = "Organización Mundial de la Salud",
            Anio = 2019,
            Resumen = "Programa de autoayuda para familiares y amigos que cuidan a una persona con demencia, en cinco módulos con ejemplos y ejercicios, que busca cuidar también la salud de quien cuida.",
            Enlace = "https://www.who.int/es/publications/i/item/9789241515863",
        },

        // por tipo de discapacidad
        new()
        {
            Titulo = "Rehabilitación multidisciplinaria para pacientes adultos con accidente cerebrovascular",
            Autores = "Lucas Alessandro, Lisandro E. Olmos, Sebastián F. Ameriso y otros",
            Fuente = "Medicina (Buenos Aires), vol. 80, n.º 1",
            Anio = 2020,
            Resumen = "Revisión de las guías de rehabilitación después de un ACV: la etapa aguda y la posterior al alta, la recuperación motora y cognitiva, la alimentación y la vuelta a las actividades de todos los días.",
            TipoDiscapacidad = "Motora, Múltiple",
            Enlace = "https://www.scielo.org.ar/scielo.php?pid=S0025-76802020000100008&script=sci_arttext",
        },
        new()
        {
            Titulo = "Experiencias de cuidadoras de niños con Distrofia Muscular de Duchenne",
            Autores = "Ximena Palacios-Espinosa y otros",
            Fuente = "Interdisciplinaria, vol. 38, n.º 3",
            Anio = 2021,
            Resumen = "Estudio con entrevistas a madres y mujeres de la familia que cuidan a niños con Duchenne: cómo viven el diagnóstico, la sobrecarga y el escaso apoyo del sistema de salud.",
            TipoDiscapacidad = "Motora",
            Enlace = "https://www.scielo.org.ar/scielo.php?script=sci_arttext&pid=S1668-70272021000300221",
        },
        new()
        {
            Titulo = "Baja visión, cómo abordarla en la práctica cotidiana",
            Autores = "Verónica López Fernández (Hospital Oftalmológico Santa Lucía)",
            Fuente = "Consejo Argentino de Oftalmología, revista MO",
            Anio = 2020,
            Resumen = "Explica qué es la baja visión y por qué la rehabilitación visual tiene que acompañar al tratamiento cuando la pérdida es permanente, con un equipo de varias disciplinas.",
            TipoDiscapacidad = "Visual",
            Enlace = "https://oftalmologos.org.ar/noticias/1048-baja-vision-practica-cotidiana",
        },
        new()
        {
            Titulo = "Discapacidad visual y ceguera",
            Autores = "Organización Mundial de la Salud",
            Fuente = "Organización Mundial de la Salud, nota descriptiva",
            Anio = 2026,
            Resumen = "Datos sobre las causas de la discapacidad visual, cómo prevenirla y qué ofrece la rehabilitación a quienes tienen una pérdida de visión que no se recupera.",
            TipoDiscapacidad = "Visual",
            Enlace = "https://www.who.int/es/news-room/fact-sheets/detail/blindness-and-visual-impairment",
        },
        new()
        {
            Titulo = "Sordera y pérdida de la audición",
            Autores = "Organización Mundial de la Salud",
            Fuente = "Organización Mundial de la Salud, nota descriptiva",
            Anio = 2026,
            Resumen = "Causas de la pérdida auditiva en cada etapa de la vida, la importancia de detectarla temprano y las opciones de rehabilitación: audífonos, implantes y lengua de señas.",
            TipoDiscapacidad = "Auditiva",
            Enlace = "https://www.who.int/es/news-room/fact-sheets/detail/deafness-and-hearing-loss",
        },
        new()
        {
            Titulo = "Calidad de vida y sobrecarga en cuidadores de escolares con discapacidad intelectual",
            Autores = "Catalina Arias Reyes y María Teresa Muñoz-Quezada",
            Fuente = "Interdisciplinaria, vol. 36, n.º 1",
            Anio = 2019,
            Resumen = "Estudio que midió la sobrecarga y la calidad de vida de quienes cuidan a escolares con discapacidad intelectual: la carga es mayor cuando la discapacidad es múltiple.",
            TipoDiscapacidad = "Intelectual",
            Enlace = "https://www.scielo.org.ar/scielo.php?pid=S1668-70272019000100017&script=sci_arttext&tlng=es",
        },
        new()
        {
            Titulo = "El autismo a lo largo de la vida",
            Autores = "Víctor Ruggieri",
            Fuente = "Medicina (Buenos Aires), vol. 82, supl. 3",
            Anio = 2022,
            Resumen = "Recorre cada etapa de la vida de una persona autista, de la detección temprana a la adultez y la vejez, y qué apoyos necesita ella y su familia en cada una.",
            TipoDiscapacidad = "Intelectual",
            Enlace = "https://www.scielo.org.ar/scielo.php?script=sci_arttext&pid=S0025-76802022000700003",
        },
        new()
        {
            Titulo = "Autismo y burnout: el agotamiento en las personas autistas, sus familias, docentes y terapeutas",
            Autores = "Víctor Ruggieri",
            Fuente = "Medicina (Buenos Aires), vol. 85, supl. 1",
            Anio = 2025,
            Resumen = "Describe el agotamiento físico y emocional que puede afectar a las personas autistas y a quienes las acompañan, y la importancia de buscar apoyo y tomarse descansos.",
            TipoDiscapacidad = "Intelectual",
            Enlace = "https://www.scielo.org.ar/scielo.php?script=sci_arttext&pid=S0025-76802025000200063",
        },
        new()
        {
            Titulo = "La ley de salud mental en Argentina",
            Autores = "Hugo Cohen",
            Fuente = "Medicina (Buenos Aires), vol. 82, n.º 5",
            Anio = 2022,
            Resumen = "Analiza la aplicación de la Ley 26.657 y sostiene que apoyar a la familia es tan prioritario como atender a la persona, porque quien cuida es clave en la recuperación.",
            TipoDiscapacidad = "Psicosocial",
            Enlace = "https://www.scielo.org.ar/scielo.php?script=sci_arttext&pid=S0025-76802022000900760",
        },
        new()
        {
            Titulo = "Informe mundial sobre la audición",
            Autores = "Organización Mundial de la Salud",
            Fuente = "Organización Panamericana de la Salud",
            Anio = 2021,
            Resumen = "Informe de la OMS sobre la pérdida de audición en el mundo, lo que cuesta no tratarla y una atención del oído y la audición centrada en las personas para que todos accedan a los servicios.",
            TipoDiscapacidad = "Auditiva",
            Enlace = "https://www.paho.org/es/documentos/informe-mundial-sobre-audicion",
        },
        new()
        {
            Titulo = "Esquizofrenia",
            Autores = "Organización Mundial de la Salud",
            Fuente = "Organización Mundial de la Salud, nota descriptiva",
            Resumen = "Qué es la esquizofrenia, qué tratamientos funcionan (medicación, rehabilitación psicosocial e intervenciones con la familia) y por qué la atención tiene que pasar del hospital psiquiátrico a la comunidad.",
            TipoDiscapacidad = "Psicosocial",
            Enlace = "https://www.who.int/es/news-room/fact-sheets/detail/schizophrenia",
        },
        new()
        {
            Titulo = "Sobrecarga del cuidador primario de pacientes con enfermedad renal crónica en terapia sustitutiva con hemodiálisis",
            Autores = "Ilse García-Arciga, Anel Gómez-García y Brenda P. Morales-Hernández",
            Fuente = "Revista Mexicana de Medicina Familiar, vol. 11, n.º 2",
            Anio = 2024,
            Resumen = "Estudio con 100 cuidadores principales de personas en hemodiálisis que midió la sobrecarga con la escala de Zarit y remarca la necesidad de cuidar la salud física y mental de quien cuida.",
            TipoDiscapacidad = "Visceral",
            Enlace = "https://scielo.org.mx/pdf/rmmf/v11n2/2007-9710-rmf-11-54.pdf",
        },
        new()
        {
            Titulo = "Enfermedades cardiovasculares",
            Autores = "Organización Mundial de la Salud",
            Fuente = "Organización Mundial de la Salud, nota descriptiva",
            Anio = 2021,
            Resumen = "Qué son las enfermedades del corazón y de los vasos sanguíneos, cuáles son sus factores de riesgo y cómo se previenen y se tratan a tiempo.",
            TipoDiscapacidad = "Visceral",
            Enlace = "https://www.who.int/es/news-room/fact-sheets/detail/cardiovascular-diseases-(cvds)",
        },
        new()
        {
            Titulo = "Beneficios de pertenecer a un grupo de apoyo de cuidadoras de niños con discapacidad múltiple",
            Autores = "María Elena Rodríguez-Vélez, Claudia Patricia Cantillo-Medina y Alix Yaneth Perdomo-Romero",
            Fuente = "Revista Peruana de Medicina Experimental y Salud Pública, vol. 41, n.º 1",
            Anio = 2024,
            Resumen = "Estudio con 20 cuidadoras que muestra cómo un grupo de apoyo funciona como red, como lugar para aprender a cuidar y a cuidarse, y como espacio para defender los derechos de los chicos.",
            TipoDiscapacidad = "Múltiple",
            Enlace = "https://www.scielosp.org/article/rpmesp/2024.v41n1/54-61/es/",
        },
    };

    // agrega los que falten (por título) sin pisar lo que haya cambiado un admin
    public static async Task CargarAsync(ApplicationDbContext context)
    {
        var titulos = await context.ArticulosRecomendados.Select(a => a.Titulo).ToListAsync();

        foreach (var articulo in Lista().Where(a => !titulos.Contains(a.Titulo)))
            context.ArticulosRecomendados.Add(articulo);

        // el de ACV sirve también para discapacidad múltiple (motora y cognitiva)
        var acv = await context.ArticulosRecomendados.FirstOrDefaultAsync(a =>
            a.Titulo.StartsWith("Rehabilitación multidisciplinaria") && a.TipoDiscapacidad == "Motora");
        if (acv != null)
            acv.TipoDiscapacidad = "Motora, Múltiple";

        await context.SaveChangesAsync();
    }
}
