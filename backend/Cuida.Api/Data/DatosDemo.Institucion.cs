using Cuida.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Data;

// Una residencia ficticia: la gerente da de alta a los residentes y mira lo que registran
// sus dos empleados. Carla anota todo; Diego deja tomas sin registrar los últimos días,
// para que el panel de la gerente tenga algo que mostrar.
public static partial class DatosDemo
{
    private const string NombreInstitucion = "Residencia Las Glicinas (demo)";

    private record ResidenteDemo(string Nombre, DateTime Nacimiento, string Tipo, string ObraSocial,
        string Diagnostico, string Empleado, MedicamentoDemo[] Medicamentos, DiarioDemo[] Diario);

    private static readonly ResidenteDemo[] Residentes =
    {
        new("Alberto Núñez", new DateTime(1943, 3, 2), "Motora", "PAMI",
            "Enfermedad de Parkinson. Usa andador.", "carla.ruiz",
            new[]
            {
                new MedicamentoDemo("Levodopa/Carbidopa 250/25 mg", "1 comprimido", "08:00,14:00,20:00", "Con el estómago vacío.", "Dr. Iván Cortés", 40),
                new MedicamentoDemo("Pramipexol 0,25 mg", "1 comprimido", "21:00", null, "Dr. Iván Cortés", null),
            },
            new[]
            {
                new DiarioDemo(1, 11, "Avance", null, "Caminó con el andador hasta el patio y volvió sin ayuda."),
                new DiarioDemo(4, 16, "Síntoma", 2, "Más rigidez a la tarde. Se lo comenté a la enfermera de turno."),
            }),
        new("Norma Castillo", new DateTime(1946, 9, 18), "Visual", "PAMI",
            "Degeneración macular asociada a la edad.", "carla.ruiz",
            new[] { new MedicamentoDemo("Enalapril 5 mg", "1 comprimido", "09:00,21:00", null, "Dra. Laura Pizarro", 12) },
            new[] { new DiarioDemo(2, 10, "Ánimo", 1, "Contenta: la visitó la nieta y escucharon radio juntas.") }),
        new("Ernesto Vidal", new DateTime(1950, 1, 27), "Visceral", "PAMI",
            "Insuficiencia renal crónica en diálisis (lunes, miércoles y viernes).", "diego.paredes",
            new[]
            {
                new MedicamentoDemo("Carbonato de calcio 500 mg", "1 comprimido", "08:00,13:00,20:00", "Con las comidas.", "Dra. Sofía Benegas", 25),
                new MedicamentoDemo("Omeprazol 20 mg", "1 cápsula", "07:30", "En ayunas.", "Dra. Sofía Benegas", null),
            },
            new[] { new DiarioDemo(6, 15, "Síntoma", 3, "Volvió muy cansado de la diálisis.") }),
        new("Susana Ortiz", new DateTime(1957, 6, 4), "Psicosocial", "IOMA",
            "Trastorno bipolar estabilizado.", "diego.paredes",
            new[] { new MedicamentoDemo("Litio 300 mg", "1 comprimido", "09:00,21:00", "Tomar mucha agua.", "Dr. Pablo Rinaldi", 8) },
            Array.Empty<DiarioDemo>()),
    };

    private static async Task CargarInstitucionAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        var gerente = await PrepararUsuarioAsync(userManager, "andrea.molina", "Andrea Molina", Planes.Plus);
        if (gerente.NombreGrupo != NombreInstitucion)
        {
            gerente.NombreGrupo = NombreInstitucion;
            await userManager.UpdateAsync(gerente);
        }

        var empleados = new Dictionary<string, ApplicationUser>
        {
            ["carla.ruiz"] = await PrepararUsuarioAsync(userManager, "carla.ruiz", "Carla Ruiz", Planes.Gratis),
            ["diego.paredes"] = await PrepararUsuarioAsync(userManager, "diego.paredes", "Diego Paredes", Planes.Gratis),
        };

        var ahora = DateTime.Now;

        foreach (var r in Residentes)
        {
            var empleado = empleados[r.Empleado];

            var persona = await context.PersonasAsistidas
                .FirstOrDefaultAsync(p => p.ResponsableId == gerente.Id && p.NombreCompleto == r.Nombre);
            if (persona != null) continue;

            persona = new PersonaAsistida
            {
                NombreCompleto = r.Nombre,
                FechaNacimiento = r.Nacimiento,
                RelacionConCuidador = "Residente",
                TipoDiscapacidad = r.Tipo,
                ObraSocial = r.ObraSocial,
                Localidad = "Pilar",
                Diagnostico = r.Diagnostico,
                ResponsableId = gerente.Id,
                ResponsableCuida = false,
            };
            context.PersonasAsistidas.Add(persona);
            await context.SaveChangesAsync();

            context.PersonasCompartidas.Add(new PersonaCompartida
            {
                PersonaAsistidaId = persona.Id,
                UsuarioId = empleado.Id,
                Rol = NivelesAcceso.Cuidador,
                FechaAlta = DateTime.UtcNow.AddDays(-60),
            });

            foreach (var demo in r.Medicamentos)
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

                        // Diego dejó de registrar algunas tomas en los últimos tres días
                        var sinRegistrar = r.Empleado == "diego.paredes" && d <= 3 && (d + horario.Length) % 2 == 0;
                        if (sinRegistrar) continue;

                        medicamento.Tomas.Add(new TomaMedicamento
                        {
                            Fecha = dia,
                            Horario = horario,
                            Estado = EstadosToma.Tomada,
                            RegistradoPorId = empleado.Id,
                            FechaRegistro = momento.ToUniversalTime().AddMinutes(r.Empleado == "carla.ruiz" ? 5 : 95),
                        });
                    }
                }

                context.Medicamentos.Add(medicamento);
            }

            foreach (var e in r.Diario)
            {
                var fecha = DateTime.Today.AddDays(-e.DiasAtras).AddHours(e.Hora);
                context.EntradasDiario.Add(new EntradaDiario
                {
                    PersonaAsistidaId = persona.Id,
                    AutorId = empleado.Id,
                    Fecha = fecha,
                    Tipo = e.Tipo,
                    Intensidad = e.Intensidad,
                    Texto = e.Texto,
                    FechaCreacion = fecha.ToUniversalTime(),
                });
            }

            context.ContactosEmergencia.Add(new ContactoEmergencia
            {
                Nombre = "Enfermería de guardia",
                Relacion = "Residencia",
                Telefono = "0230 445-0000",
                TipoContacto = "Institución",
                PersonaAsistidaId = persona.Id,
                CuidadorId = gerente.Id,
            });

            await context.SaveChangesAsync();
        }
    }
}
