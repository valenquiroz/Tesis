using Cuida.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Data;

// Mantiene la demo al día: al arrancar completa las tomas de la última semana y corre el diario
// de prueba para que lo más nuevo quede en los últimos días. Solo toca a las personas de las
// cuentas de demostración. Se activa con Demo:Renovar (appsettings.Development.json) o al cargar la demo.
public static partial class DatosDemo
{
    private const string CorreoDiego = "diegoparedes@gmail.com";
    private const string CorreoCarla = "carlaruiz@gmail.com";

    public static async Task RenovarAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue<bool>("Demo:Renovar") && !configuration.GetValue<bool>("Demo:Cargar"))
            return;

        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatosDemo");

        var correos = Familias().Select(f => EmailDe(f.Nombre)).Append(EmailDe("Andrea Molina")).ToList();
        var duenos = await context.Users.Where(u => correos.Contains(u.Email!)).Select(u => u.Id).ToListAsync();
        if (duenos.Count == 0) return;

        var personas = await context.PersonasAsistidas
            .Where(p => duenos.Contains(p.ResponsableId))
            .Select(p => new
            {
                p.Id,
                p.ResponsableId,
                p.ResponsableCuida,
                // quien registra: el primer cuidador del equipo o, si no hay, el responsable
                Cuidador = context.PersonasCompartidas
                    .Where(c => c.PersonaAsistidaId == p.Id && c.Rol == NivelesAcceso.Cuidador)
                    .OrderBy(c => c.FechaAlta)
                    .Select(c => new { c.UsuarioId, c.Usuario!.Email })
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var ahora = DateTime.Now;
        var desde = DateTime.Today.AddDays(-7);
        var agregadas = 0;

        foreach (var persona in personas)
        {
            var registraId = persona.Cuidador?.UsuarioId ?? (persona.ResponsableCuida ? persona.ResponsableId : null);
            if (registraId == null) continue;
            var correo = persona.Cuidador?.Email;

            var medicamentos = await context.Medicamentos
                .Where(m => m.PersonaAsistidaId == persona.Id && m.Activo)
                .Select(m => new { m.Id, m.Nombre, m.Horarios, Tomas = m.Tomas.Where(t => t.Fecha >= desde).Select(t => new { t.Fecha, t.Horario }).ToList() })
                .ToListAsync();

            foreach (var m in medicamentos)
            {
                for (var d = 7; d >= 0; d--)
                {
                    var dia = DateTime.Today.AddDays(-d);
                    foreach (var horario in m.Horarios.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var momento = dia.Add(TimeSpan.Parse(horario));
                        if (momento > ahora.AddMinutes(-30)) continue;
                        if (m.Tomas.Any(t => t.Fecha == dia && t.Horario == horario)) continue;

                        // Diego deja tomas sin registrar en los últimos días y anota tarde
                        if (correo == CorreoDiego && d <= 3 && (d + horario.Length) % 2 == 0) continue;

                        var omitida = correo != CorreoDiego && correo != CorreoCarla && (d + horario.Length + m.Nombre.Length) % 9 == 0;
                        var demora = correo == CorreoDiego ? 95 : correo == CorreoCarla ? 5 : 10;

                        context.TomasMedicamentos.Add(new TomaMedicamento
                        {
                            MedicamentoId = m.Id,
                            Fecha = dia,
                            Horario = horario,
                            Estado = omitida ? EstadosToma.Omitida : EstadosToma.Tomada,
                            RegistradoPorId = registraId,
                            FechaRegistro = momento.ToUniversalTime().AddMinutes(demora),
                        });
                        agregadas++;
                    }
                }
            }

            // el diario de prueba se corre en bloque para que lo último quede de ayer
            var ultima = await context.EntradasDiario
                .Where(e => e.PersonaAsistidaId == persona.Id)
                .MaxAsync(e => (DateTime?)e.Fecha);
            if (ultima is DateTime fecha)
            {
                var dias = (DateTime.Today.AddDays(-1) - fecha.Date).Days;
                if (dias > 0)
                {
                    await context.EntradasDiario
                        .Where(e => e.PersonaAsistidaId == persona.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(e => e.Fecha, e => e.Fecha.AddDays(dias))
                            .SetProperty(e => e.FechaCreacion, e => e.FechaCreacion.AddDays(dias)));
                }
            }
        }

        await context.SaveChangesAsync();
        if (agregadas > 0)
            logger.LogInformation("Demo al día: {Agregadas} tomas de la última semana.", agregadas);
    }
}
