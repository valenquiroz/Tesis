using System.Globalization;
using System.Text;
using Cuida.Api.Data;
using Cuida.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Services;

// Recomendador híbrido:
// - por contenido: compara el perfil de la persona con cada recurso (de ahí sale el % de coincidencia);
// - por valoraciones: lo que el cuidador marcó como útil sube y lo que no, baja al final;
//   y lo que les sirvió a otros cuidadores con el mismo tipo de discapacidad suma puntos.
public class RecomendacionService
{
    public const double PesoDiscapacidad = 40;
    public const double PesoObraSocial = 30;
    public const double PesoLocalidad = 30;
    public const double PesoNecesidades = 35;

    // valoraciones
    public const double PesoValoracionPropia = 30;
    public const double PesoComunidad = 20;
    // con menos votos que esto el aporte de la comunidad se achica (evita que un voto mueva todo)
    public const int MinimoVotos = 3;

    public const double PuntajeMaximo =
        PesoDiscapacidad + PesoObraSocial + PesoLocalidad + PesoNecesidades;

    // ya normalizados (minúsculas, sin tildes)
    private static readonly Dictionary<string, string> Sinonimos = new()
    {
        ["motriz"] = "motora",
        ["fisica"] = "motora",
        ["mental"] = "intelectual",
        ["cognitiva"] = "intelectual",
        ["sensorial visual"] = "visual",
        ["ceguera"] = "visual",
        ["sordera"] = "auditiva",
        ["hipoacusia"] = "auditiva",
    };

    private readonly ApplicationDbContext _context;

    public RecomendacionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecomendacionResultado>> ObtenerRecomendaciones(
        int personaAsistidaId)
    {
        var persona = await _context.PersonasAsistidas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personaAsistidaId);

        if (persona == null)
        {
            return new List<RecomendacionResultado>();
        }

        var recursos = await _context.RecursosRecomendables
            .AsNoTracking()
            .Where(r => r.Activo)
            .ToListAsync();

        var necesidadesPersona = SepararLista(persona.Necesidades);

        var propias = await _context.ValoracionesRecursos
            .AsNoTracking()
            .Where(v => v.PersonaAsistidaId == personaAsistidaId)
            .ToDictionaryAsync(v => v.RecursoRecomendableId, v => v.Util);

        var votosSimilares = await VotosDePersonasSimilaresAsync(persona);

        var resultados = new List<RecomendacionResultado>();

        foreach (var recurso in recursos)
        {
            double puntuacion = 0;
            var motivos = new List<string>();

            var necesidadesRecurso = SepararLista(recurso.Necesidades);

            // cada recurso es para uno o más tipos de discapacidad ("Motora, Múltiple"): solo se
            // recomienda a quien tiene alguno de esos tipos. Lo que sirve a todos (CUD, pensión,
            // transporte) está en la guía de trámites.
            if (!SepararLista(recurso.TipoDiscapacidad).Any(t => Coinciden(persona.TipoDiscapacidad, t)))
                continue;

            // un recurso de PAMI no sirve a quien tiene otra obra social, ni uno de Rosario
            // a alguien de Escobar: si el recurso pide algo que la persona no cumple, se descarta
            if (!ObraSocialCorresponde(persona.ObraSocial, recurso.ObraSocial) ||
                NoCorresponde(persona.Localidad, recurso.Localidad))
                continue;

            puntuacion += PesoDiscapacidad;
            motivos.Add($"Coincide con el tipo de discapacidad: {persona.TipoDiscapacidad}");

            if (!string.IsNullOrWhiteSpace(recurso.ObraSocial))
            {
                puntuacion += PesoObraSocial;
                motivos.Add(EsSinObraSocial(recurso.ObraSocial)
                    ? "Es para quien no tiene obra social"
                    : $"Coincide con la obra social: {persona.ObraSocial}");
            }

            if (Coinciden(persona.Localidad, recurso.Localidad))
            {
                puntuacion += PesoLocalidad;
                motivos.Add($"Está disponible en {persona.Localidad}");
            }

            var coincidenciasNecesidad = necesidadesRecurso
                .Where(n => necesidadesPersona.Any(p => Coinciden(p, n)))
                .ToList();

            if (coincidenciasNecesidad.Count > 0)
            {
                puntuacion += PesoNecesidades;
                motivos.Add(
                    $"Responde a la necesidad: {string.Join(", ", coincidenciasNecesidad)}");
            }

            // el % y el nivel muestran la coincidencia con el perfil; las valoraciones solo reordenan
            var porContenido = puntuacion;

            if (votosSimilares.TryGetValue(recurso.Id, out var votos))
            {
                var neto = votos.Utiles - (votos.Total - votos.Utiles);
                puntuacion += PesoComunidad * Math.Clamp((double)neto / Math.Max(votos.Total, MinimoVotos), -1, 1);

                if (votos.Utiles > 0)
                    motivos.Add(MotivoComunidad(votos.Utiles, persona.TipoDiscapacidad));
            }

            bool? valoracion = propias.TryGetValue(recurso.Id, out var util) ? util : null;

            if (valoracion == true)
            {
                puntuacion += PesoValoracionPropia;
                motivos.Insert(0, "Marcaste que te sirvió");
            }

            resultados.Add(new RecomendacionResultado
            {
                Recurso = recurso,
                Puntuacion = puntuacion,
                Porcentaje = (int)Math.Round(porContenido / PuntajeMaximo * 100),
                Nivel = ClasificarNivel(porContenido),
                Motivos = motivos,
                Valoracion = valoracion,
                VotosUtiles = votos.Utiles
            });
        }

        // lo que la persona marcó como "no me sirvió" va al final, sin desaparecer (se puede deshacer)
        return resultados
            .OrderBy(r => r.Valoracion == false)
            .ThenByDescending(r => r.Puntuacion)
            .ThenBy(r => r.Recurso.Titulo)
            .ToList();
    }

    // Votos de otros cuidadores cuya persona tiene el mismo tipo de discapacidad
    // (o de todos, si la persona no lo tiene cargado).
    private async Task<Dictionary<int, (int Utiles, int Total)>> VotosDePersonasSimilaresAsync(
        PersonaAsistida persona)
    {
        var valoraciones = await _context.ValoracionesRecursos
            .AsNoTracking()
            .Where(v => v.PersonaAsistidaId != persona.Id)
            .Select(v => new
            {
                v.RecursoRecomendableId,
                v.Util,
                v.PersonaAsistida!.TipoDiscapacidad
            })
            .ToListAsync();

        var sinTipo = string.IsNullOrWhiteSpace(persona.TipoDiscapacidad);

        return valoraciones
            .Where(v => sinTipo || Coinciden(persona.TipoDiscapacidad, v.TipoDiscapacidad))
            .GroupBy(v => v.RecursoRecomendableId)
            .ToDictionary(g => g.Key, g => (g.Count(v => v.Util), g.Count()));
    }

    private static string MotivoComunidad(int utiles, string? tipoDiscapacidad)
    {
        var quienes = utiles == 1
            ? "A 1 persona que cuida"
            : $"A {utiles} personas que cuidan";
        var aQuien = string.IsNullOrWhiteSpace(tipoDiscapacidad)
            ? "a alguien"
            : $"a alguien con discapacidad {tipoDiscapacidad.Trim().ToLowerInvariant()}";

        return $"{quienes} {aQuien} {(utiles == 1 ? "le" : "les")} sirvió";
    }

    private static string ClasificarNivel(double puntuacion)
    {
        if (puntuacion >= 60) return "Alta";
        if (puntuacion >= 30) return "Media";
        return "Baja";
    }

    private static bool Coinciden(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        return Canonico(a) == Canonico(b);
    }

    // "Sin obra social" en un recurso: solo para quien no tiene obra social o se atiende por
    // Incluir Salud (el programa para titulares de pensiones no contributivas)
    private static readonly HashSet<string> SinObraSocial = new() { "sin obra social", "ninguna", "no tiene", "incluir salud" };

    private static bool EsSinObraSocial(string? valor) =>
        string.IsNullOrWhiteSpace(valor) || SinObraSocial.Contains(Normalizar(valor));

    private static bool ObraSocialCorresponde(string? dePersona, string? deRecurso) =>
        string.IsNullOrWhiteSpace(deRecurso) ||
        (EsSinObraSocial(deRecurso) ? EsSinObraSocial(dePersona) : Coinciden(dePersona, deRecurso));

    // el recurso exige un valor y la persona no lo tiene (o tiene otro)
    private static bool NoCorresponde(string? valorPersona, string? valorRecurso) =>
        !string.IsNullOrWhiteSpace(valorRecurso) && !Coinciden(valorPersona, valorRecurso);

    private static string Canonico(string texto)
    {
        var normalizado = Normalizar(texto);

        // "Discapacidad motora" → "motora"
        if (normalizado.StartsWith("discapacidad "))
            normalizado = normalizado["discapacidad ".Length..];

        return Sinonimos.TryGetValue(normalizado, out var equivalente)
            ? equivalente
            : normalizado;
    }

    // "Córdoba" == "cordoba" == "  CORDOBA "
    public static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);

        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return string.Join(' ',
            sb.ToString().Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<string> SepararLista(string? valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? new List<string>()
            : valor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
}

public class RecomendacionResultado
{
    public RecursoRecomendable Recurso { get; set; } = null!;

    public double Puntuacion { get; set; }

    // 0 a 100
    public int Porcentaje { get; set; }

    public string Nivel { get; set; } = string.Empty;

    public List<string> Motivos { get; set; } = new();

    // lo que marcó quien cuida a esta persona: true = me sirvió, false = no me sirvió
    public bool? Valoracion { get; set; }

    // a cuántos cuidadores de personas con el mismo tipo de discapacidad les sirvió
    public int VotosUtiles { get; set; }
}
