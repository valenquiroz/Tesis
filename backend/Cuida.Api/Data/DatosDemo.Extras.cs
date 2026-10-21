using Cuida.Api.Controllers;
using Cuida.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Data;

// Ficha de emergencia, medicación, diario, bienestar y equipos de cuidado de la demo.
// Cada bloque se carga solo si la persona (o el usuario) todavía no tiene nada de eso.
public static partial class DatosDemo
{
    private record FichaDemo(string? Grupo, string? Alergias, string? Diagnostico, string? Afiliado,
        string? Medico, string? TelefonoMedico, string? Informacion, DateTime? VenceCud);

    private record MedicamentoDemo(string Nombre, string Dosis, string Horarios, string? Indicaciones,
        string? Medico, int? DiasVenceReceta);

    private record DiarioDemo(int DiasAtras, int Hora, string Tipo, int? Intensidad, string Texto, string? Autor = null);

    private static readonly Dictionary<string, FichaDemo> Fichas = new()
    {
        ["Rosa Fernández"] = new("A+", "Penicilina", "Secuela de ACV isquémico (2021) con hemiparesia izquierda. Hipertensión arterial.",
            "PAMI 15-123456-7-01", "Dr. Gustavo Paredes", "0230 442-1180",
            "Entiende todo pero habla despacio: darle tiempo para responder. Se angustia si la apuran.", null),
        ["Tomás Ledesma"] = new("0+", null, "Discapacidad intelectual moderada. Trastorno del espectro autista.",
            "IOMA 4-0912-3341/02", "Dra. Paula Cejas (pediatra)", "0221 427-5500",
            "No le gustan los ruidos fuertes ni que lo toquen sin avisar. Se calma con sus auriculares y su tablet.",
            DateTime.Today.AddMonths(5)),
        ["Héctor Benítez"] = new("B+", "Ninguna conocida", "Glaucoma crónico bilateral con baja visión.",
            "PAMI 15-998877-1-00", "Dr. Ernesto Salvi", "011 4902-3311",
            "Presentarse por el nombre al acercarse: no reconoce caras. Vive solo.", DateTime.Today.AddMonths(14)),
        ["Martín Sosa"] = new("0-", "Ibuprofeno", "Trastorno bipolar tipo I.", "Incluir Salud 20-27654321-9",
            "Dra. Julieta Ríos (psiquiatra)", "0341 448-0200",
            "Si está en crisis, hablarle con calma y llamar primero a su psiquiatra.", null),
        ["Elena Gómez"] = new("A-", "Sulfas", "Hipoacusia bilateral. Artrosis de cadera. Hipotiroidismo.",
            "PAMI 15-445566-3-00", "Dra. Mónica Ferrari", "0348 442-6600",
            "Hablarle de frente y despacio: lee los labios. Con audífono escucha bastante mejor.", null),
        ["Micaela Paz"] = new("AB+", "Látex", "Parálisis cerebral espástica (diplejía).", "OSECAC 30-1122334-5",
            "Dr. Leandro Funes (neuropediatra)", "011 4749-2200",
            "Se comunica con gestos y un tablero de pictogramas. Le encanta la música: la tranquiliza.",
            DateTime.Today.AddDays(45)),
        ["Osvaldo Villalba"] = new("B-", "Contraste iodado", "Insuficiencia renal crónica estadio 5 en hemodiálisis.",
            "PAMI 15-332211-8-00", "Dra. Graciela Molina (nefróloga)", "011 4743-9010",
            "Dializa lunes, miércoles y viernes de 7 a 11. No tomar en el brazo izquierdo la presión (fístula).", null),
    };

    private static readonly Dictionary<string, MedicamentoDemo[]> MedicamentosDemo = new()
    {
        ["Rosa Fernández"] = new[]
        {
            new MedicamentoDemo("Enalapril 10 mg", "1 comprimido", "08:00,20:00", "Con un vaso de agua.", "Dr. Gustavo Paredes", 12),
            new MedicamentoDemo("Aspirina 100 mg", "1 comprimido", "13:00", "Después del almuerzo.", "Dr. Gustavo Paredes", 60),
        },
        ["Tomás Ledesma"] = new[]
        {
            new MedicamentoDemo("Melatonina 3 mg", "1 comprimido", "21:00", "Media hora antes de dormir.", "Dra. Paula Cejas", null),
        },
        ["Héctor Benítez"] = new[]
        {
            new MedicamentoDemo("Timolol 0,5 % (gotas)", "1 gota en cada ojo", "08:00,20:00", "Esperar 5 minutos entre una gota y otra.", "Dr. Ernesto Salvi", 40),
            new MedicamentoDemo("Latanoprost (gotas)", "1 gota en cada ojo", "21:00", "Guardar en la heladera.", "Dr. Ernesto Salvi", 40),
        },
        ["Martín Sosa"] = new[]
        {
            new MedicamentoDemo("Carbonato de litio 300 mg", "1 comprimido", "09:00,21:00", "Control de litemia cada 3 meses.", "Dra. Julieta Ríos", 5),
        },
        ["Elena Gómez"] = new[]
        {
            new MedicamentoDemo("Levotiroxina 50 mcg", "1 comprimido", "07:00", "En ayunas, media hora antes del desayuno.", "Dra. Mónica Ferrari", 90),
        },
        ["Micaela Paz"] = new[]
        {
            new MedicamentoDemo("Baclofeno 10 mg", "1 comprimido", "08:00,14:00,20:00", null, "Dr. Leandro Funes", 25),
        },
        ["Osvaldo Villalba"] = new[]
        {
            new MedicamentoDemo("Carbonato de calcio 500 mg", "1 comprimido", "12:00,20:00", "Con las comidas.", "Dra. Graciela Molina", 30),
            new MedicamentoDemo("Amlodipina 5 mg", "1 comprimido", "08:00", "Los días de diálisis, después de la sesión.", "Dra. Graciela Molina", 30),
        },
    };

    private static readonly Dictionary<string, DiarioDemo[]> DiarioDemoPorPersona = new()
    {
        ["Rosa Fernández"] = new[]
        {
            new DiarioDemo(1, 18, "Síntoma", 2, "Se cansó caminando al almacén, tuvimos que parar dos veces. Volvimos en remis."),
            new DiarioDemo(6, 10, "Avance", null, "En kinesio logró subir los tres escalones con la baranda sola. Estaba contenta."),
            new DiarioDemo(13, 22, "Sueño", 3, "Se despertó dos veces a la noche con dolor en el hombro izquierdo."),
        },
        ["Tomás Ledesma"] = new[]
        {
            new DiarioDemo(2, 8, "Crisis", 4, "Crisis antes de ir a la escuela: cambiaron el transporte y no lo sabía. Se calmó con los auriculares en 20 minutos."),
            new DiarioDemo(5, 20, "Avance", null, "Armó solo el rompecabezas de 24 piezas y pidió \"otro\" con palabras."),
            new DiarioDemo(9, 13, "Alimentación", 2, "Solo quiso comer fideos blancos. Rechazó todo lo demás."),
            new DiarioDemo(16, 9, "Crisis", 3, "Se tapó los oídos y lloró en la sala de espera del pediatra por el ruido."),
        },
        ["Martín Sosa"] = new[]
        {
            new DiarioDemo(1, 21, "Ánimo", 2, "Estuvo callado y durmió la siesta larga. Dice que está cansado."),
            new DiarioDemo(4, 11, "Ánimo", 4, "Muy acelerado, habló sin parar toda la mañana y durmió 4 horas. Le avisé a la Dra. Ríos."),
            new DiarioDemo(11, 19, "Avance", null, "Retomó el taller de carpintería del centro de día."),
        },
        ["Elena Gómez"] = new[]
        {
            new DiarioDemo(1, 12, "Alimentación", 1, "Comió bien, pero hay que cortarle la carne en trozos chicos."),
            new DiarioDemo(3, 17, "Síntoma", 3, "Dolor de cadera al levantarse de la cama. Le costó bastante más que de costumbre."),
            new DiarioDemo(8, 10, "Ánimo", 2, "Estuvo triste: se acordó de papá todo el día."),
        },
        ["Micaela Paz"] = new[]
        {
            new DiarioDemo(2, 16, "Avance", null, "En la fono pidió música con el tablero de pictogramas, sin ayuda."),
            new DiarioDemo(7, 23, "Sueño", 3, "Las valvas le molestaron y se despertó llorando. Las sacamos a las 3."),
        },
        ["Osvaldo Villalba"] = new[]
        {
            new DiarioDemo(1, 13, "Síntoma", 3, "Salió muy cansado de la diálisis y con calambres en las piernas."),
            new DiarioDemo(5, 9, "Alimentación", 2, "Se comió una banana a escondidas. Le expliqué otra vez lo del potasio."),
        },
    };

    // Equipos: Jorge contrató a Marta para cuidar a su mamá y a su hermano, y él y su
    // hermana Lorena miran lo que ella registra. Marta también cuida a Micaela (otra familia).
    private static readonly (string Responsable, string Miembro, string NombreMiembro, string Rol, string[] Personas)[] Equipos =
    {
        ("jgomez.escobar", "marta.suarez", "Marta Suárez", NivelesAcceso.Cuidador, new[] { "Elena Gómez", "Raúl Gómez" }),
        ("jgomez.escobar", "lorena.gomez", "Lorena Gómez", NivelesAcceso.Observador, new[] { "Elena Gómez", "Raúl Gómez" }),
        ("silvia.acuna", "marta.suarez", "Marta Suárez", NivelesAcceso.Cuidador, new[] { "Micaela Paz" }),
    };

    // personas cuyo responsable solo mira (modo observador)
    private static readonly string[] ResponsableSoloMira = { "Elena Gómez", "Raúl Gómez" };

    // (usuario, meses atrás, respuestas afirmativas por índice de pregunta)
    private static readonly (string Usuario, int MesesAtras, int[] Si)[] EvaluacionesDemo =
    {
        ("graciela.ibarra", 6, new[] { 1, 2, 3, 11 }),
        ("graciela.ibarra", 0, new[] { 0, 1, 2, 3, 4, 5, 9, 11 }),
        ("jgomez.escobar", 1, new[] { 0, 1, 2, 3, 5, 6, 7, 11, 12 }),
        ("fedeledesma", 0, new[] { 1, 3, 8, 11 }),
    };

    private static async Task CargarExtrasAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        Dictionary<string, ApplicationUser> usuarios)
    {
        var personas = await context.PersonasAsistidas
            .Where(p => usuarios.Values.Select(u => u.Id).Contains(p.ResponsableId))
            .ToListAsync();

        // Ficha de emergencia (solo si está vacía)
        foreach (var persona in personas)
        {
            if (!Fichas.TryGetValue(persona.NombreCompleto, out var ficha) || persona.Diagnostico != null) continue;

            persona.GrupoSanguineo = ficha.Grupo;
            persona.Alergias = ficha.Alergias;
            persona.Diagnostico = ficha.Diagnostico;
            persona.NumeroAfiliado = ficha.Afiliado;
            persona.MedicoCabecera = ficha.Medico;
            persona.TelefonoMedico = ficha.TelefonoMedico;
            persona.InformacionEmergencia = ficha.Informacion;
            persona.VencimientoCud = ficha.VenceCud;

            if (ficha.VenceCud is DateTime vence)
            {
                context.Turnos.Add(new Turno
                {
                    PersonaAsistidaId = persona.Id,
                    CuidadorId = persona.ResponsableId,
                    Origen = PersonasAsistidasController.OrigenVencimientoCud,
                    Titulo = $"Renovar el CUD de {persona.NombreCompleto.Split(' ')[0]}",
                    FechaHora = (vence.AddDays(-60) < DateTime.Today ? vence : vence.AddDays(-60)).AddHours(9),
                    Especialidad = "Trámite",
                    Lugar = "Junta evaluadora",
                    Observaciones = $"El certificado vence el {vence:dd/MM/yyyy}. Para renovarlo hay que volver a la junta con el certificado vencido.",
                });
            }
        }
        await context.SaveChangesAsync();

        // Equipos de cuidado (si ya existían, se corrige el rol)
        foreach (var (responsable, miembro, nombre, rol, nombresPersonas) in Equipos)
        {
            if (!usuarios.TryGetValue(responsable, out var usuarioResponsable)) continue;

            if (!usuarios.TryGetValue(miembro, out var usuarioMiembro))
            {
                usuarioMiembro = await PrepararUsuarioAsync(userManager, miembro, nombre, Planes.Gratis);
                usuarios[miembro] = usuarioMiembro;
            }

            foreach (var persona in personas.Where(p => p.ResponsableId == usuarioResponsable.Id && nombresPersonas.Contains(p.NombreCompleto)))
            {
                var actual = await context.PersonasCompartidas
                    .FirstOrDefaultAsync(c => c.PersonaAsistidaId == persona.Id && c.UsuarioId == usuarioMiembro.Id);
                if (actual == null)
                {
                    context.PersonasCompartidas.Add(new PersonaCompartida
                    {
                        PersonaAsistidaId = persona.Id,
                        UsuarioId = usuarioMiembro.Id,
                        Rol = rol,
                        FechaAlta = DateTime.UtcNow.AddDays(-40),
                    });
                }
                else
                {
                    actual.Rol = rol;
                }
            }
        }

        foreach (var persona in personas)
            persona.ResponsableCuida = !ResponsableSoloMira.Contains(persona.NombreCompleto);

        await context.SaveChangesAsync();

        // quién registra en cada persona: el responsable, o la cuidadora si el responsable solo mira
        var registra = new Dictionary<int, string>();
        foreach (var persona in personas)
        {
            var cuidador = persona.ResponsableCuida
                ? null
                : await context.PersonasCompartidas
                    .Where(c => c.PersonaAsistidaId == persona.Id && c.Rol == NivelesAcceso.Cuidador)
                    .Select(c => c.UsuarioId)
                    .FirstOrDefaultAsync();
            registra[persona.Id] = cuidador ?? persona.ResponsableId;
        }

        // demos cargadas antes del modo observador: lo que figuraba a nombre de la familia pasa a la cuidadora
        foreach (var persona in personas.Where(p => !p.ResponsableCuida))
        {
            var cuidadoraId = registra[persona.Id];
            if (cuidadoraId == persona.ResponsableId) continue;

            var familia = await context.PersonasCompartidas
                .Where(c => c.PersonaAsistidaId == persona.Id && c.Rol == NivelesAcceso.Observador)
                .Select(c => c.UsuarioId)
                .ToListAsync();
            familia.Add(persona.ResponsableId);

            await context.TomasMedicamentos
                .Where(t => t.Medicamento!.PersonaAsistidaId == persona.Id && familia.Contains(t.RegistradoPorId))
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RegistradoPorId, cuidadoraId));
            await context.EntradasDiario
                .Where(e => e.PersonaAsistidaId == persona.Id && familia.Contains(e.AutorId))
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.AutorId, cuidadoraId));
        }

        // Medicación y tomas de la última semana
        var ahora = DateTime.Now;
        foreach (var persona in personas)
        {
            if (!MedicamentosDemo.TryGetValue(persona.NombreCompleto, out var lista)) continue;
            if (await context.Medicamentos.AnyAsync(m => m.PersonaAsistidaId == persona.Id)) continue;

            foreach (var demo in lista)
            {
                var medicamento = new Medicamento
                {
                    PersonaAsistidaId = persona.Id,
                    Nombre = demo.Nombre,
                    Dosis = demo.Dosis,
                    Horarios = demo.Horarios,
                    Indicaciones = demo.Indicaciones,
                    Medico = demo.Medico,
                    VencimientoReceta = demo.DiasVenceReceta is int dias ? DateTime.Today.AddDays(dias) : null,
                    FechaCreacion = DateTime.UtcNow.AddDays(-30),
                };

                for (var d = 7; d >= 0; d--)
                {
                    var dia = DateTime.Today.AddDays(-d);
                    foreach (var horario in demo.Horarios.Split(','))
                    {
                        var momento = dia.Add(TimeSpan.Parse(horario));
                        if (momento > ahora.AddMinutes(-30)) continue;

                        // de vez en cuando una toma salteada, para que el resumen tenga algo que mostrar
                        var omitida = (d + horario.Length + demo.Nombre.Length) % 9 == 0;
                        medicamento.Tomas.Add(new TomaMedicamento
                        {
                            Fecha = dia,
                            Horario = horario,
                            Estado = omitida ? EstadosToma.Omitida : EstadosToma.Tomada,
                            RegistradoPorId = registra[persona.Id],
                            FechaRegistro = momento.ToUniversalTime().AddMinutes(10),
                        });
                    }
                }

                context.Medicamentos.Add(medicamento);
            }
        }
        await context.SaveChangesAsync();

        // Diario
        foreach (var persona in personas)
        {
            if (!DiarioDemoPorPersona.TryGetValue(persona.NombreCompleto, out var entradas)) continue;
            if (await context.EntradasDiario.AnyAsync(e => e.PersonaAsistidaId == persona.Id)) continue;

            foreach (var e in entradas)
            {
                var autor = e.Autor != null && usuarios.TryGetValue(e.Autor, out var u) ? u.Id : registra[persona.Id];
                var fecha = DateTime.Today.AddDays(-e.DiasAtras).AddHours(e.Hora);
                context.EntradasDiario.Add(new EntradaDiario
                {
                    PersonaAsistidaId = persona.Id,
                    AutorId = autor,
                    Fecha = fecha,
                    Tipo = e.Tipo,
                    Intensidad = e.Intensidad,
                    Texto = e.Texto,
                    FechaCreacion = fecha.ToUniversalTime(),
                });
            }
        }
        await context.SaveChangesAsync();

        // Autoevaluaciones de esfuerzo
        foreach (var grupo in EvaluacionesDemo.GroupBy(e => e.Usuario))
        {
            if (!usuarios.TryGetValue(grupo.Key, out var usuario)) continue;
            if (await context.EvaluacionesEsfuerzo.AnyAsync(e => e.UsuarioId == usuario.Id)) continue;

            foreach (var (_, mesesAtras, si) in grupo)
            {
                var respuestas = Enumerable.Range(0, 13).Select(i => si.Contains(i) ? 1 : 0).ToArray();
                context.EvaluacionesEsfuerzo.Add(new EvaluacionEsfuerzo
                {
                    UsuarioId = usuario.Id,
                    Fecha = DateTime.UtcNow.AddMonths(-mesesAtras).AddDays(-2),
                    Respuestas = string.Join(',', respuestas),
                    Puntaje = si.Length,
                });
            }
        }
        await context.SaveChangesAsync();
    }
}
