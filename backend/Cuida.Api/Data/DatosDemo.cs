using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Data;

// Datos de demostración: varias familias con distintos planes (1 o 2 personas a cargo),
// una cuidadora contratada que atiende a dos familias, una observadora y una institución
// con su gerente y empleados. Se cargan con Demo:Cargar = true.
// Cada cuenta: nombreapellido@gmail.com y contraseña Nombre123! (ver docs/cuentas-demo.md).
public static partial class DatosDemo
{
    // dominio que se usaba antes; esas cuentas se renombran conservando sus datos
    private const string DominioAnterior = "acompanar.demo";

    public static async Task CargarAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>("Demo:Cargar"))
            return;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatosDemo");
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var usuarios = new Dictionary<string, ApplicationUser>();

        foreach (var familia in Familias())
        {
            var plan = PlanesDemo.GetValueOrDefault(familia.Usuario, Planes.Gratis);
            var usuario = await PrepararUsuarioAsync(userManager, familia.Usuario, familia.Nombre, plan);
            usuarios[familia.Usuario] = usuario;
            await CargarPersonasAsync(context, usuario.Id, familia.Personas);
            await CargarTramitesAsync(context, usuario.Id);
        }

        await EliminarEspecialistasAsync(userManager);
        await CargarExtrasAsync(context, userManager, usuarios);
        await CargarInstitucionAsync(context, userManager);

        logger.LogInformation("Datos de demostración cargados.");
    }

    // ------------------------------------------------------------------
    // Cuentas
    // ------------------------------------------------------------------

    // "Lic. Mariana Ferreyra" → marianaferreyra@gmail.com
    public static string EmailDe(string nombre) =>
        RecomendacionService.Normalizar(Formato.SinTitulo(nombre)).Replace(" ", "") + "@gmail.com";

    // "Lic. Mariana Ferreyra" → Mariana123!
    public static string ClaveDe(string nombre) =>
        Formato.SinTitulo(nombre).Split(' ')[0] + "123!";

    // si la cuenta ya existe se le vuelve a poner la contraseña de demo, el plan y el rol
    private static async Task<ApplicationUser> PrepararUsuarioAsync(
        UserManager<ApplicationUser> userManager, string usuarioDemo, string nombre, string plan,
        string rol = "Cuidador")
    {
        var email = EmailDe(nombre);
        var password = ClaveDe(nombre);

        var usuario = await userManager.FindByEmailAsync(email)
            ?? await userManager.FindByEmailAsync($"{usuarioDemo}@{DominioAnterior}");

        if (usuario != null && usuario.Email != email)
        {
            await userManager.SetEmailAsync(usuario, email);
            await userManager.SetUserNameAsync(usuario, email);
        }

        if (usuario == null)
        {
            usuario = new ApplicationUser { UserName = email, Email = email };
            usuario.NombreCompleto = nombre;
            usuario.Plan = plan;

            var creado = await userManager.CreateAsync(usuario, password);
            if (!creado.Succeeded)
                throw new InvalidOperationException(string.Join(" ", creado.Errors.Select(e => e.Description)));
        }
        else
        {
            usuario.NombreCompleto = nombre;
            usuario.Plan = plan;
            await userManager.UpdateAsync(usuario);

            var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            await userManager.ResetPasswordAsync(usuario, token, password);
        }

        if (!await userManager.IsInRoleAsync(usuario, rol))
            await userManager.AddToRoleAsync(usuario, rol);

        return usuario;
    }

    // qué plan tiene cada familia de la demo (las demás, Gratis)
    private static readonly Dictionary<string, string> PlanesDemo = new()
    {
        ["graciela.ibarra"] = Planes.Plus,
        ["nbenitez"] = Planes.Equipo,
        ["caro.sosa"] = Planes.Equipo,
        ["jgomez.escobar"] = Planes.Equipo,
        ["silvia.acuna"] = Planes.Equipo,
    };

    // La sección de especialistas se sacó: sus cuentas de demo ya no existen
    private static readonly string[] EspecialistasAnteriores =
    {
        "Lic. Mariana Ferreyra", "Dra. Silvina Etchegaray", "Lic. Renata Albornoz", "Lic. Lucas Medina",
    };

    private static async Task EliminarEspecialistasAsync(UserManager<ApplicationUser> userManager)
    {
        foreach (var nombre in EspecialistasAnteriores)
        {
            var usuario = await userManager.FindByEmailAsync(EmailDe(nombre));
            if (usuario != null)
                await userManager.DeleteAsync(usuario);
        }
    }

    // ------------------------------------------------------------------
    // Familias: cuidador, personas a cargo, turnos y contactos
    // ------------------------------------------------------------------

    private record TurnoDemo(int Dias, int Hora, int Minuto, string Titulo,
        string? Especialidad = null, string? Profesional = null, string? Lugar = null, string? Observaciones = null);

    private record ContactoDemo(string Nombre, string Relacion, string Telefono, string Tipo);

    private record PersonaDemo(PersonaAsistida Persona, TurnoDemo[] Turnos, ContactoDemo[] Contactos);

    private record FamiliaDemo(string Usuario, string Nombre, PersonaDemo[] Personas);

    private static string Necesidades(params string[] lista) => string.Join(", ", lista);

    private static FamiliaDemo[] Familias() => new[]
    {
        new FamiliaDemo("graciela.ibarra", "Graciela Ibarra", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Rosa Fernández",
                    FechaNacimiento = new DateTime(1949, 5, 12),
                    RelacionConCuidador = "Madre",
                    TipoDiscapacidad = "Motora",
                    ObraSocial = "PAMI",
                    Localidad = "Pilar",
                    Domicilio = "Av. Tomás Márquez 1240, Pilar",
                    Necesidades = Necesidades(DbInitializer.NecesidadTransporte, DbInitializer.NecesidadRehabilitacion),
                    Observaciones = "Usa andador para trayectos largos. Hipertensión controlada con enalapril (mañana y noche)."
                },
                new[]
                {
                    new TurnoDemo(-6, 10, 0, "Control clínico", "Clínica médica", "Dr. Ariel Paz", "Hospital Austral, consultorio 14"),
                    new TurnoDemo(0, 16, 30, "Kinesiología", "Kinesiología", "Lic. Sergio Almada", "Centro de rehabilitación Pilar"),
                    new TurnoDemo(3, 16, 30, "Kinesiología", "Kinesiología", "Lic. Sergio Almada", "Centro de rehabilitación Pilar"),
                    new TurnoDemo(9, 9, 0, "Renovación del CUD", null, null, "Junta evaluadora, Municipalidad de Pilar",
                        "Llevar DNI, CUD anterior y estudios de los últimos 6 meses."),
                    new TurnoDemo(17, 11, 15, "Control de presión", "Cardiología", "Dra. Lucía Benavídez", "Hospital Austral"),
                },
                new[]
                {
                    new ContactoDemo("Norma Ibarra", "Hermana", "11 5555-1234", "Familiar"),
                    new ContactoDemo("Dr. Ariel Paz", "Médico de cabecera", "0230 442-1100", "Profesional"),
                }),
        }),

        new FamiliaDemo("fedeledesma", "Federico Ledesma", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Tomás Ledesma",
                    FechaNacimiento = new DateTime(2016, 9, 3),
                    RelacionConCuidador = "Hijo",
                    TipoDiscapacidad = "Intelectual",
                    ObraSocial = "IOMA",
                    Localidad = "La Plata",
                    Domicilio = "Calle 47 n.º 812, La Plata",
                    Necesidades = Necesidades(DbInitializer.NecesidadTramites, DbInitializer.NecesidadRehabilitacion),
                    Observaciones = "Va a escuela especial a la mañana. Le cuestan los cambios de rutina: avisarle con tiempo."
                },
                new[]
                {
                    new TurnoDemo(-3, 15, 0, "Fonoaudiología", "Fonoaudiología", "Lic. Carla Ruiz", "Consultorio calle 12 n.º 1530"),
                    new TurnoDemo(1, 15, 0, "Fonoaudiología", "Fonoaudiología", "Lic. Carla Ruiz", "Consultorio calle 12 n.º 1530"),
                    new TurnoDemo(2, 8, 30, "Reunión con la escuela", null, "Equipo de orientación", "Escuela de Educación Especial n.º 503"),
                    new TurnoDemo(12, 10, 0, "Control neurológico", "Neurología infantil", "Dra. Silvina Etchegaray",
                        "Hospital de Niños de La Plata", "Llevar el cuaderno de registro de conductas."),
                    new TurnoDemo(24, 17, 0, "Terapia ocupacional", "Terapia ocupacional", "Lic. Mariana Ferreyra", "CET La Plata"),
                },
                new[]
                {
                    new ContactoDemo("Analía Torres", "Mamá de Tomás", "221 456-7890", "Familiar"),
                    new ContactoDemo("Escuela Especial n.º 503", "Dirección", "221 421-3344", "Institución"),
                }),
        }),

        new FamiliaDemo("nbenitez", "Natalia Benítez", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Héctor Benítez",
                    FechaNacimiento = new DateTime(1944, 11, 20),
                    RelacionConCuidador = "Padre",
                    TipoDiscapacidad = "Visual",
                    ObraSocial = "PAMI",
                    Localidad = "Ciudad Autónoma de Buenos Aires",
                    Domicilio = "Av. Rivadavia 5320, 4.º B, Caballito",
                    Necesidades = Necesidades(DbInitializer.NecesidadTransporte, DbInitializer.NecesidadCobertura),
                    Observaciones = "Baja visión por glaucoma. Gotas cada 12 horas. Vive solo, lo visitamos día por medio."
                },
                new[]
                {
                    new TurnoDemo(-10, 11, 0, "Control oftalmológico", "Oftalmología", "Dr. Rubén Olmedo", "Hospital Santa Lucía"),
                    new TurnoDemo(4, 9, 30, "Campo visual", "Oftalmología", null, "Hospital Santa Lucía", "Ir con acompañante."),
                    new TurnoDemo(15, 14, 0, "Rehabilitación visual", "Rehabilitación", "Lic. Julia Herrera", "Centro de rehabilitación visual"),
                },
                new[]
                {
                    new ContactoDemo("Graciela (vecina)", "Vecina del 4.º A", "11 4987-2210", "Otro"),
                    new ContactoDemo("Hospital Santa Lucía", "Guardia", "11 4127-3100", "Institución"),
                }),
        }),

        new FamiliaDemo("pablo.romero.cba", "Pablo Romero", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Lucía Romero",
                    FechaNacimiento = new DateTime(1990, 2, 14),
                    RelacionConCuidador = "Hermana",
                    TipoDiscapacidad = "Auditiva",
                    ObraSocial = "OSDE",
                    Localidad = "Córdoba",
                    Domicilio = "Bv. San Juan 455, Córdoba",
                    Necesidades = Necesidades(DbInitializer.NecesidadTramites, DbInitializer.NecesidadEconomica),
                    Observaciones = "Usa audífonos. Se comunica por LSA y lectura labial."
                },
                new[]
                {
                    new TurnoDemo(6, 12, 0, "Calibración de audífonos", "Fonoaudiología", "Lic. Nora Peralta", "Centro auditivo Córdoba"),
                    new TurnoDemo(21, 10, 30, "Control ORL", "Otorrinolaringología", "Dr. Esteban Ríos", "Clínica Reina Fabiola"),
                },
                new[]
                {
                    new ContactoDemo("Ana María Godoy", "Madre", "351 555-0099", "Familiar"),
                }),
        }),

        new FamiliaDemo("caro.sosa", "Carolina Vázquez", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Martín Sosa",
                    FechaNacimiento = new DateTime(1978, 7, 8),
                    RelacionConCuidador = "Esposo",
                    TipoDiscapacidad = "Psicosocial",
                    ObraSocial = "Incluir Salud",
                    Localidad = "Rosario",
                    Domicilio = "Bv. Oroño 1850, Rosario",
                    Necesidades = Necesidades(DbInitializer.NecesidadEconomica, DbInitializer.NecesidadCobertura),
                    Observaciones = "Tratamiento psiquiátrico. Controlar que retire la medicación del mes."
                },
                new[]
                {
                    new TurnoDemo(-1, 18, 0, "Psicoterapia", "Psicología", "Lic. Lucas Medina", "Consultorio Bv. Oroño 1200"),
                    new TurnoDemo(6, 18, 0, "Psicoterapia", "Psicología", "Lic. Lucas Medina", "Consultorio Bv. Oroño 1200"),
                    new TurnoDemo(13, 18, 0, "Psicoterapia", "Psicología", "Lic. Lucas Medina", "Consultorio Bv. Oroño 1200"),
                    new TurnoDemo(10, 9, 0, "Control psiquiátrico", "Psiquiatría", "Dra. Paula Méndez", "Hospital Provincial de Rosario",
                        "Pedir receta de la medicación para dos meses."),
                },
                new[]
                {
                    new ContactoDemo("Dra. Paula Méndez", "Psiquiatra", "341 480-2200", "Profesional"),
                    new ContactoDemo("Jorge Sosa", "Hermano de Martín", "341 615-7788", "Familiar"),
                }),
        }),

        new FamiliaDemo("jgomez.escobar", "Jorge Gómez", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Elena Gómez",
                    FechaNacimiento = new DateTime(1938, 3, 30),
                    RelacionConCuidador = "Madre",
                    TipoDiscapacidad = "Múltiple",
                    ObraSocial = "PAMI",
                    Localidad = "Escobar",
                    Domicilio = "Mitre 330, Belén de Escobar",
                    Necesidades = Necesidades(DbInitializer.NecesidadCobertura, DbInitializer.NecesidadCercania),
                    Observaciones = "Movilidad reducida e hipoacusia. Necesita ayuda para bañarse y vestirse."
                },
                new[]
                {
                    new TurnoDemo(-2, 10, 0, "Visita de enfermería", "Enfermería", null, "Domicilio"),
                    new TurnoDemo(2, 11, 30, "Control geriátrico", "Geriatría", "Dr. Martín Ocampo", "Hospital Erill, Escobar"),
                    new TurnoDemo(5, 10, 0, "Visita de enfermería", "Enfermería", null, "Domicilio"),
                    new TurnoDemo(27, 9, 0, "Entrevista centro de día", null, null, "Centro de día municipal de Escobar",
                        "Llevar certificado médico y copia del CUD."),
                },
                new[]
                {
                    new ContactoDemo("Liliana Gómez", "Hermana", "11 3344-5566", "Familiar"),
                    new ContactoDemo("PAMI Escobar", "Agencia", "0348 442-6000", "Institución"),
                }),
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Raúl Gómez",
                    FechaNacimiento = new DateTime(1971, 1, 9),
                    RelacionConCuidador = "Hermano",
                    TipoDiscapacidad = "Motora",
                    ObraSocial = "PAMI",
                    Localidad = "Escobar",
                    Domicilio = "Mitre 330, Belén de Escobar",
                    Necesidades = Necesidades(DbInitializer.NecesidadTransporte),
                    Observaciones = "Usa silla de ruedas desde un accidente en 2015. Es bastante independiente en la casa."
                },
                new[]
                {
                    new TurnoDemo(7, 14, 30, "Control de traumatología", "Traumatología", "Dr. Hugo Sánchez", "Hospital Erill, Escobar"),
                    new TurnoDemo(19, 10, 0, "Revisión de la silla de ruedas", null, null, "Ortopedia Escobar"),
                },
                new[]
                {
                    new ContactoDemo("Liliana Gómez", "Hermana", "11 3344-5566", "Familiar"),
                }),
        }),

        new FamiliaDemo("silvia.acuna", "Silvia Acuña", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Micaela Paz",
                    FechaNacimiento = new DateTime(2011, 6, 22),
                    RelacionConCuidador = "Hija",
                    TipoDiscapacidad = "Motora",
                    ObraSocial = "OSECAC",
                    Localidad = "Tigre",
                    Domicilio = "Av. Cazón 1567, Tigre",
                    Necesidades = Necesidades(DbInitializer.NecesidadRehabilitacion, DbInitializer.NecesidadTransporte),
                    Observaciones = "Parálisis cerebral. Usa valvas por la noche. Le encanta la música."
                },
                new[]
                {
                    new TurnoDemo(-4, 17, 0, "Hidroterapia", "Kinesiología", "Lic. Daniela Funes", "Club Náutico Tigre"),
                    new TurnoDemo(3, 17, 0, "Hidroterapia", "Kinesiología", "Lic. Daniela Funes", "Club Náutico Tigre"),
                    new TurnoDemo(8, 9, 30, "Control de ortopedia", "Ortopedia infantil", "Dr. Iván Correa", "Hospital Garrahan",
                        "Llevar las valvas para que las revise."),
                    new TurnoDemo(10, 17, 0, "Hidroterapia", "Kinesiología", "Lic. Daniela Funes", "Club Náutico Tigre"),
                },
                new[]
                {
                    new ContactoDemo("Hernán Paz", "Papá de Micaela", "11 6012-7745", "Familiar"),
                    new ContactoDemo("Hospital Garrahan", "Turnos", "11 4122-6000", "Institución"),
                }),
        }),

        new FamiliaDemo("ramiro.villalba", "Ramiro Villalba", new[]
        {
            new PersonaDemo(
                new PersonaAsistida
                {
                    NombreCompleto = "Osvaldo Villalba",
                    FechaNacimiento = new DateTime(1946, 8, 15),
                    RelacionConCuidador = "Abuelo",
                    TipoDiscapacidad = "Visceral",
                    ObraSocial = "PAMI",
                    Localidad = "San Isidro",
                    Domicilio = "Belgrano 412, San Isidro",
                    Necesidades = Necesidades(DbInitializer.NecesidadTransporte, DbInitializer.NecesidadCobertura),
                    Observaciones = "Insuficiencia renal. Diálisis lunes, miércoles y viernes. Dieta baja en sal y potasio."
                },
                new[]
                {
                    new TurnoDemo(-5, 7, 30, "Diálisis", "Nefrología", null, "Centro de diálisis San Isidro"),
                    new TurnoDemo(0, 7, 30, "Diálisis", "Nefrología", null, "Centro de diálisis San Isidro"),
                    new TurnoDemo(2, 7, 30, "Diálisis", "Nefrología", null, "Centro de diálisis San Isidro"),
                    new TurnoDemo(4, 7, 30, "Diálisis", "Nefrología", null, "Centro de diálisis San Isidro"),
                    new TurnoDemo(14, 12, 0, "Control nefrológico", "Nefrología", "Dra. Mónica Arce", "Hospital Central de San Isidro",
                        "Llevar los últimos análisis de sangre."),
                },
                new[]
                {
                    new ContactoDemo("Centro de diálisis San Isidro", "Enfermería", "11 4747-3300", "Institución"),
                    new ContactoDemo("Andrea Villalba", "Tía", "11 5899-0012", "Familiar"),
                }),
        }),
    };

    // cómo va cada familia con los trámites (solo se carga si la persona no tiene ninguno)
    private static readonly Dictionary<string, (string Codigo, string Estado)[]> AvanceTramites = new()
    {
        ["Rosa Fernández"] = new[]
        {
            ("cud-consulta", EstadosTramite.Hecho), ("cud-planilla", EstadosTramite.Hecho),
            ("estudios-motora", EstadosTramite.EnCurso),
        },
        ["Tomás Ledesma"] = new[]
        {
            ("cud-consulta", EstadosTramite.Hecho), ("cud-planilla", EstadosTramite.Hecho),
            ("estudios-mental", EstadosTramite.Hecho), ("cud-junta", EstadosTramite.Hecho),
            ("cobertura-100", EstadosTramite.Hecho), ("asignacion-hijo", EstadosTramite.EnCurso),
        },
        ["Héctor Benítez"] = new[] { ("cud-consulta", EstadosTramite.Hecho) },
        ["Lucía Romero"] = new[]
        {
            ("cud-consulta", EstadosTramite.Hecho), ("cud-planilla", EstadosTramite.Hecho),
            ("estudios-auditiva", EstadosTramite.Hecho), ("cud-junta", EstadosTramite.EnCurso),
        },
        ["Osvaldo Villalba"] = new[]
        {
            ("cud-consulta", EstadosTramite.Hecho), ("cud-planilla", EstadosTramite.Hecho),
            ("estudios-visceral", EstadosTramite.Hecho), ("cud-junta", EstadosTramite.Hecho),
            ("cobertura-100", EstadosTramite.EnCurso), ("pension-invalidez", EstadosTramite.NoCorresponde),
        },
    };

    private static async Task CargarTramitesAsync(ApplicationDbContext context, string cuidadorId)
    {
        var personas = await context.PersonasAsistidas
            .Where(p => p.ResponsableId == cuidadorId)
            .Select(p => new { p.Id, p.NombreCompleto })
            .ToListAsync();

        foreach (var persona in personas)
        {
            if (!AvanceTramites.TryGetValue(persona.NombreCompleto, out var avance)) continue;
            if (await context.TramitesPersona.AnyAsync(t => t.PersonaAsistidaId == persona.Id)) continue;

            var dias = avance.Length * 6;
            foreach (var (codigo, estado) in avance)
            {
                context.TramitesPersona.Add(new TramitePersona
                {
                    PersonaAsistidaId = persona.Id,
                    Codigo = codigo,
                    Estado = estado,
                    FechaActualizacion = DateTime.UtcNow.AddDays(-dias),
                });
                dias -= 6;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task CargarPersonasAsync(ApplicationDbContext context, string cuidadorId, PersonaDemo[] personas)
    {
        var existentes = await context.PersonasAsistidas
            .Where(p => p.ResponsableId == cuidadorId)
            .Select(p => p.NombreCompleto)
            .ToListAsync();

        foreach (var demo in personas)
        {
            if (existentes.Contains(demo.Persona.NombreCompleto))
                continue;

            var persona = demo.Persona;
            persona.ResponsableId = cuidadorId;
            context.PersonasAsistidas.Add(persona);
            await context.SaveChangesAsync();

            foreach (var t in demo.Turnos)
            {
                context.Turnos.Add(new Turno
                {
                    Titulo = t.Titulo,
                    FechaHora = DateTime.Today.AddDays(t.Dias).AddHours(t.Hora).AddMinutes(t.Minuto),
                    Especialidad = t.Especialidad,
                    Profesional = t.Profesional,
                    Lugar = t.Lugar,
                    Observaciones = t.Observaciones,
                    PersonaAsistidaId = persona.Id,
                    CuidadorId = cuidadorId
                });
            }

            foreach (var c in demo.Contactos)
            {
                context.ContactosEmergencia.Add(new ContactoEmergencia
                {
                    Nombre = c.Nombre,
                    Relacion = c.Relacion,
                    Telefono = c.Telefono,
                    TipoContacto = c.Tipo,
                    PersonaAsistidaId = persona.Id,
                    CuidadorId = cuidadorId
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
