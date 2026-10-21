using Cuida.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Data;

// Seed: roles, admin (si está configurado), recursos y artículos. Se puede correr en cada arranque.
public static class DbInitializer
{
    // Cuidador = cuenta particular (familia, cuidador contratado u observador)
    public static readonly string[] Roles = { "Cuidador", "Administrador" };

    // roles de versiones anteriores que ya no se usan
    private static readonly string[] RolesRetirados = { "Especialista", "Gerente" };

    public static async Task InicializarAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        await CrearRolesAsync(roleManager, userManager);
        await CrearAdministradorAsync(userManager, configuration, logger);
        await CargarRecursosAsync(context);
        await ArticulosIniciales.CargarAsync(context);
    }

    private static async Task CrearRolesAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        foreach (var rol in Roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        // quienes tenían un rol retirado (por ejemplo, gerente de una institución) siguen
        // entrando como cualquier cuenta
        foreach (var rol in RolesRetirados)
        {
            var viejo = await roleManager.FindByNameAsync(rol);
            if (viejo == null) continue;

            foreach (var usuario in await userManager.GetUsersInRoleAsync(rol))
            {
                if (!await userManager.IsInRoleAsync(usuario, "Cuidador"))
                    await userManager.AddToRoleAsync(usuario, "Cuidador");
            }

            await roleManager.DeleteAsync(viejo);
        }
    }

    // Admin:Email y Admin:Password van en appsettings.Local.json
    private static async Task CrearAdministradorAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var admin = await userManager.FindByEmailAsync(email);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NombreCompleto = configuration["Admin:NombreCompleto"] ?? "Administrador"
            };

            var resultado = await userManager.CreateAsync(admin, password);

            if (!resultado.Succeeded)
            {
                logger.LogWarning(
                    "No se pudo crear el administrador: {Errores}",
                    string.Join(" ", resultado.Errors.Select(e => e.Description)));
                return;
            }
        }

        // también Cuidador, si no el panel le da 403
        foreach (var rol in new[] { "Administrador", "Cuidador" })
        {
            if (!await userManager.IsInRoleAsync(admin, rol))
                await userManager.AddToRoleAsync(admin, rol);
        }
    }

    // mismos textos que NECESIDADES en utilidades.js
    public const string NecesidadTramites = "Trámites y certificados";
    public const string NecesidadCobertura = "Cobertura de salud";
    public const string NecesidadEconomica = "Apoyo económico";
    public const string NecesidadTransporte = "Transporte";
    public const string NecesidadRehabilitacion = "Rehabilitación";
    public const string NecesidadCercania = "Recursos cerca de casa";

    // Cada recurso es para uno o más tipos de discapacidad (separados por coma) y cada enlace
    // lleva a la página que lo describe (revisados en octubre de 2026). Lo que sirve a cualquier
    // persona (CUD, pensión, transporte) está en la guía de trámites.
    private static List<RecursoRecomendable> RecursosIniciales() => new()
    {
        // motora
        new()
        {
            Titulo = "Rehabilitación y ayudas técnicas que cubre la obra social",
            Descripcion =
                "Qué prestaciones tienen que cubrir las obras sociales con el CUD: rehabilitación, prótesis, ayudas técnicas, medicamentos y transporte (Ley 24.901).",
            Categoria = "Orientación",
            TipoDiscapacidad = "Motora, Múltiple",
            Necesidades = $"{NecesidadRehabilitacion}, {NecesidadCobertura}",
            Enlace = "https://www.argentina.gob.ar/justicia/derechofacil/leysimple/prestaciones-por-discapacidad"
        },
        new()
        {
            Titulo = "Símbolo Internacional de Acceso",
            Descripcion =
                "Cómo tramitar en Mi Argentina el Símbolo Internacional de Acceso digital si tenés el CUD, para estacionar y circular con el vehículo que traslada a la persona.",
            Categoria = "Transporte",
            TipoDiscapacidad = "Motora, Múltiple",
            Necesidades = $"{NecesidadTransporte}, {NecesidadTramites}",
            Enlace = "https://www.argentina.gob.ar/obtener-simbolo-internacional-de-acceso-digital"
        },
        new()
        {
            Titulo = "Registro Nacional de Prestadores",
            Descripcion =
                "Qué es el registro de prestadores de atención y rehabilitación para personas con discapacidad y dónde consultar las instituciones inscriptas.",
            Categoria = "Prestadores",
            TipoDiscapacidad = "Motora, Intelectual, Múltiple",
            Necesidades = NecesidadRehabilitacion,
            Enlace = "https://www.argentina.gob.ar/servicio/registro-nacional-de-prestadores-de-servicio-de-atencion-y-rehabilitacion-para-personas-con"
        },
        new()
        {
            Titulo = "Asistencia a personas con discapacidad en PAMI",
            Descripcion =
                "Centro de día, club de día, pequeño hogar o residencia para afiliados de PAMI con discapacidad: documentación y cómo pedirlo.",
            Categoria = "Cobertura",
            TipoDiscapacidad = "Motora, Intelectual, Múltiple",
            ObraSocial = "PAMI",
            Necesidades = NecesidadCobertura,
            Enlace = "https://www.pami.org.ar/tramite/asistencia-personas-discapacidad"
        },
        new()
        {
            Titulo = "Dirección de Inclusión de Pilar",
            Descripcion =
                "Área del Municipio de Pilar para personas con discapacidad: programas, actividades inclusivas y contacto.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Motora",
            Localidad = "Pilar",
            Necesidades = NecesidadCercania,
            Enlace = "https://pilar.gov.ar/comunidad/discapacidad/"
        },
        new()
        {
            Titulo = "Centro de Rehabilitación Pilares de Esperanza (Pilar)",
            Descripcion =
                "Centro municipal de rehabilitación para personas con discapacidad neuromotora: kinesiología, fisiatría, terapia ocupacional y fonoaudiología, entre otras.",
            Categoria = "Rehabilitación",
            TipoDiscapacidad = "Motora",
            Localidad = "Pilar",
            Necesidades = NecesidadRehabilitacion,
            Enlace = "https://pilar.gov.ar/comunidad/discapacidad/"
        },
        new()
        {
            Titulo = "Hospital de Rehabilitación Sangalli Dupuy (Escobar)",
            Descripcion =
                "Hospital municipal de rehabilitación neuromotora en Matheu, con internación y un equipo de fisiatría, kinesiología, terapia ocupacional, neurología y psicología.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Motora",
            Localidad = "Escobar",
            Necesidades = $"{NecesidadRehabilitacion}, {NecesidadCercania}",
            Enlace = "https://www.escobar.gob.ar/centros-de-salud/"
        },
        new()
        {
            Titulo = "Centro de Rehabilitación Juana Azurduy (Tigre)",
            Descripcion =
                "Centro municipal para adultos que necesitan rehabilitación psicofísica o cardiovascular: kinesiología, terapia ocupacional, fonoaudiología y psicología. También es sede de la junta del CUD.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Motora, Visceral",
            Localidad = "Tigre",
            Necesidades = $"{NecesidadRehabilitacion}, {NecesidadCercania}",
            Enlace = "https://www.tigre.gob.ar/salud/hospital/349"
        },
        new()
        {
            Titulo = "Centro de Rehabilitación Frida Kahlo (Tigre)",
            Descripcion =
                "Centro municipal en General Pacheco para niñas y niños con problemas del desarrollo y neurolocomotores: diagnóstico, rehabilitación y elementos ortopédicos para quienes no tienen obra social.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Motora, Intelectual, Múltiple",
            Localidad = "Tigre",
            Necesidades = $"{NecesidadRehabilitacion}, {NecesidadCercania}",
            Enlace = "https://www.tigre.gob.ar/salud/hospital/339"
        },

        // visual
        new()
        {
            Titulo = "Biblioteca Argentina para Ciegos",
            Descripcion =
                "Biblioteca con libros en braille y audiolibros para sus socios, imprenta braille y venta de elementos como bastones y pizarras, con sede en Almagro (CABA).",
            Categoria = "Orientación",
            TipoDiscapacidad = "Visual",
            Necesidades = NecesidadRehabilitacion,
            Enlace = "https://www.bac.org.ar/"
        },
        new()
        {
            Titulo = "Baja visión y rehabilitación visual en el Hospital Santa Lucía",
            Descripcion =
                "Qué es la baja visión y cómo es la rehabilitación visual en el Hospital de Oftalmología Santa Lucía de la Ciudad de Buenos Aires.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Visual",
            Localidad = "Ciudad Autónoma de Buenos Aires",
            Necesidades = $"{NecesidadRehabilitacion}, {NecesidadCercania}",
            Enlace = "https://buenosaires.gob.ar/gcaba_historico/hospitalsantalucia/baja-vision-y-rehabilitacion-visual"
        },

        // auditiva
        new()
        {
            Titulo = "Cobertura de audífonos e implante coclear",
            Descripcion =
                "La obra social tiene que cubrir el tratamiento de la hipoacusia, los audífonos, las prótesis auditivas, la rehabilitación y el implante coclear (Ley 25.415).",
            Categoria = "Orientación",
            TipoDiscapacidad = "Auditiva",
            Necesidades = NecesidadCobertura,
            Enlace = "https://www.argentina.gob.ar/justicia/derechofacil/leysimple/hipoacusia"
        },
        new()
        {
            Titulo = "Curso de Lengua de Señas Argentina en Córdoba",
            Descripcion =
                "Curso libre y gratuito de LSA de la Municipalidad de Córdoba, en tres niveles de marzo a noviembre.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Auditiva",
            Localidad = "Córdoba",
            Necesidades = NecesidadCercania,
            Enlace = "https://cordoba.gob.ar/curso-basico-de-lengua-de-senas-argentina-abierto-y-gratuito/"
        },

        // intelectual
        new()
        {
            Titulo = "Inclusión educativa",
            Descripcion =
                "Qué es la modalidad de educación especial y cómo asegura el derecho a la educación de estudiantes con discapacidad en todo el sistema educativo.",
            Categoria = "Educación",
            TipoDiscapacidad = "Intelectual",
            Necesidades = NecesidadTramites,
            Enlace = "https://www.argentina.gob.ar/modalidad-de-educacion-especial"
        },
        new()
        {
            Titulo = "Dirección de Educación Especial bonaerense (La Plata)",
            Descripcion =
                "Dirección de la Provincia de Buenos Aires, con sede en La Plata, a cargo de la educación de niñas, niños y jóvenes con discapacidad.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Intelectual",
            Localidad = "La Plata",
            Necesidades = $"{NecesidadRehabilitacion}, {NecesidadCercania}",
            Enlace = "https://abc.gob.ar/secretarias/areas/subsecretaria-de-educacion/educacion-especial/educacion-especial/direccion-de-educacion"
        },

        // psicosocial
        new()
        {
            Titulo = "Atención en salud mental",
            Descripcion =
                "Derechos de las personas usuarias de servicios de salud mental y cómo debe ser la atención (Ley 26.657).",
            Categoria = "Salud",
            TipoDiscapacidad = "Psicosocial",
            Necesidades = NecesidadCobertura,
            Enlace = "https://www.argentina.gob.ar/justicia/derechofacil/leysimple/salud-mental"
        },
        new()
        {
            Titulo = "Centros de salud municipales de Rosario",
            Descripcion =
                "Listado de centros de salud y hospitales municipales de Rosario, con dirección y WhatsApp, para pedir atención en salud mental cerca de casa.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Psicosocial",
            Localidad = "Rosario",
            Necesidades = NecesidadCercania,
            Enlace = "https://www.rosario.gob.ar/inicio/centros-de-salud-y-hospitales"
        },

        // visceral
        new()
        {
            Titulo = "Derechos de las personas trasplantadas y en lista de espera",
            Descripcion =
                "Cobertura del 100 % de los medicamentos y estudios vinculados al trasplante, traslado prioritario con un acompañante y libre elección del centro de trasplante (Ley 27.447).",
            Categoria = "Orientación",
            TipoDiscapacidad = "Visceral",
            Necesidades = NecesidadCobertura,
            Enlace = "https://www.argentina.gob.ar/justicia/derechofacil/leysimple/trasplante-de-organos"
        },
        new()
        {
            Titulo = "Hospital Central de San Isidro",
            Descripcion =
                "Hospital municipal con consultorios de nefrología, cardiología y neumología, donde también se inicia el trámite del CUD en San Isidro.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Visceral",
            Localidad = "San Isidro",
            Necesidades = NecesidadCercania,
            Enlace = "https://www.sanisidro.gob.ar/hospital-central"
        },

        // múltiple
        new()
        {
            Titulo = "Centros de día de Escobar",
            Descripcion =
                "Guía municipal de servicios de salud de Escobar, que incluye el Centro de Día René Favaloro y el Centro de Día PAMI.",
            Categoria = "Recursos locales",
            TipoDiscapacidad = "Múltiple",
            Localidad = "Escobar",
            Necesidades = NecesidadCercania,
            Enlace = "https://www.escobar.gob.ar/centros-de-salud/"
        }
    };

    // eran para cualquier persona: ahora están en la guía de trámites y salen de Recursos
    private static readonly string[] TitulosGenerales =
    {
        "Certificado Único de Discapacidad (CUD)",
        "Pensión No Contributiva por Invalidez Laboral",
        "Programa Federal Incluir Salud",
        "Transporte para personas con discapacidad",
    };

    // títulos con los que se cargaron en la primera versión
    private static readonly Dictionary<string, string> TitulosAnteriores = new()
    {
        ["Asistencia a personas con discapacidad en PAMI"] = "Información de prestaciones para PAMI",
        ["Dirección de Inclusión de Pilar"] = "Recursos para discapacidad motora en Pilar",
        ["Centro de Rehabilitación Pilares de Esperanza (Pilar)"] = "Rehabilitación motora en Pilar",
        ["Rehabilitación y ayudas técnicas que cubre la obra social"] = "Orientación para personas con discapacidad visual",
        ["Cobertura de audífonos e implante coclear"] = "Recursos para personas sordas e hipoacúsicas",
        ["Baja visión y rehabilitación visual en el Hospital Santa Lucía"] = "Rehabilitación visual en la Ciudad de Buenos Aires",
        ["Curso de Lengua de Señas Argentina en Córdoba"] = "Talleres de Lengua de Señas en Córdoba",
        ["Dirección de Educación Especial bonaerense (La Plata)"] = "Centro educativo terapéutico en La Plata",
        ["Centros de salud municipales de Rosario"] = "Acompañamiento en salud mental en Rosario",
        ["Centros de día de Escobar"] = "Centro de día para personas mayores en Escobar",
    };

    // enlaces de versiones anteriores (genéricos o que ya no existen):
    // un recurso que todavía tiene uno se actualiza entero
    private static readonly HashSet<string> EnlacesViejos = new()
    {
        "https://www.argentina.gob.ar/",
        "https://www.argentina.gob.ar/andis",
        "https://www.argentina.gob.ar/andis/cud",
        "https://www.argentina.gob.ar/andis/transporte",
        "https://www.argentina.gob.ar/andis/simbolo-internacional-de-acceso",
        "https://www.argentina.gob.ar/servicio/pension-no-contributiva-por-invalidez-laboral",
        "https://www.argentina.gob.ar/salud",
        "https://www.argentina.gob.ar/educacion",
        "https://www.argentina.gob.ar/salud/incluirsalud",
        "https://www.pami.org.ar/",
    };

    // duplicados de la versión más vieja del seed: se desactivan si siguen con el enlace genérico
    private static readonly string[] TitulosRetirados =
    {
        "Centro de rehabilitación motora de Pilar",
        "Información sobre beneficios de PAMI",
        "Recurso para personas con discapacidad motora en Pilar",
    };

    // agrega los que falten y corrige los de la primera versión, sin pisar lo que haya cambiado un admin
    private static async Task CargarRecursosAsync(ApplicationDbContext context)
    {
        var existentes = await context.RecursosRecomendables.ToListAsync();

        foreach (var recurso in RecursosIniciales())
        {
            var actual = existentes.FirstOrDefault(r => r.Titulo == recurso.Titulo);
            if (actual == null && TitulosAnteriores.TryGetValue(recurso.Titulo, out var anterior))
                actual = existentes.FirstOrDefault(r => r.Titulo == anterior);

            if (actual == null)
            {
                context.RecursosRecomendables.Add(recurso);
                continue;
            }

            if (actual.Enlace == null || EnlacesViejos.Contains(actual.Enlace))
            {
                actual.Titulo = recurso.Titulo;
                actual.Descripcion = recurso.Descripcion;
                actual.Categoria = recurso.Categoria;
                actual.TipoDiscapacidad = recurso.TipoDiscapacidad;
                actual.ObraSocial = recurso.ObraSocial;
                actual.Localidad = recurso.Localidad;
                actual.Enlace = recurso.Enlace;
                actual.Necesidades = recurso.Necesidades;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(actual.Necesidades))
                    actual.Necesidades = recurso.Necesidades;

                // los que se cargaron como generales pasan a ser de uno o más tipos de discapacidad
                if (string.IsNullOrWhiteSpace(actual.TipoDiscapacidad))
                {
                    actual.TipoDiscapacidad = recurso.TipoDiscapacidad;
                    actual.Descripcion = recurso.Descripcion;
                }
            }
        }

        // lo que sirve a cualquier persona quedó en la guía de trámites
        foreach (var general in existentes.Where(r =>
                     TitulosGenerales.Contains(r.Titulo) && string.IsNullOrWhiteSpace(r.TipoDiscapacidad)))
        {
            general.Activo = false;
        }

        foreach (var viejo in existentes.Where(r =>
                     TitulosRetirados.Contains(r.Titulo) && r.Enlace != null && EnlacesViejos.Contains(r.Enlace)))
        {
            viejo.Activo = false;
        }

        await context.SaveChangesAsync();
    }
}
