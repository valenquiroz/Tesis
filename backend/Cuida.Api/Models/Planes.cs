namespace Cuida.Api.Models;

// Planes de la plataforma. Quien se registra es dueño de su grupo: carga a las personas
// que cuida y puede sumar cuidadores y observadores, cada uno con su cuenta.
// - Gratis: lo básico del día a día (tu día, medicación, calendario, documentos). La emergencia va en todos.
// - Equipo: suma trámites, diario, recursos recomendados, tu bienestar y gente al grupo.
// - Plus: todo, sin límites, con los artículos y el panel para supervisar a cada cuidador.
// En el prototipo el cambio de plan se simula: no hay cobro real.
public static class Planes
{
    public const string Gratis = "Gratis";
    public const string Equipo = "Equipo";
    public const string Plus = "Plus";

    public static readonly string[] Todos = { Gratis, Equipo, Plus };

    private const long Mb = 1024L * 1024;

    // null = sin límite
    public static readonly IReadOnlyDictionary<string, LimitesPlan> Limites = new Dictionary<string, LimitesPlan>
    {
        [Gratis] = new(Gratis, "Básico", 0, Personas: 1, Integrantes: 0, EspacioDocumentos: 100 * Mb,
            Tramites: false, Diario: false, Recursos: false, Articulos: false, Bienestar: false, Supervision: false, Informe: false),
        [Equipo] = new(Equipo, "Intermedio", 5, Personas: 5, Integrantes: 3, EspacioDocumentos: 1024 * Mb,
            Tramites: true, Diario: true, Recursos: true, Articulos: false, Bienestar: true, Supervision: false, Informe: false),
        [Plus] = new(Plus, "Plus", 15, Personas: null, Integrantes: null, EspacioDocumentos: 10 * 1024 * Mb,
            Tramites: true, Diario: true, Recursos: true, Articulos: true, Bienestar: true, Supervision: true, Informe: true),
    };

    public static LimitesPlan De(string? plan) =>
        plan != null && Limites.TryGetValue(plan, out var limites) ? limites : Limites[Gratis];

    // el mejor de dos planes
    public static LimitesPlan Mayor(LimitesPlan a, LimitesPlan b) =>
        Array.IndexOf(Todos, a.Codigo) >= Array.IndexOf(Todos, b.Codigo) ? a : b;

    // el plan más barato que incluye una función, para los avisos
    public static string QueIncluye(Func<LimitesPlan, bool> incluye) =>
        Todos.Select(De).First(incluye).Nombre;
}

public record LimitesPlan(
    string Codigo,
    string Nombre,
    decimal PrecioMensualUsd,
    int? Personas,
    int? Integrantes,
    long? EspacioDocumentos,
    bool Tramites,
    bool Diario,
    bool Recursos,
    bool Articulos,
    bool Bienestar,
    bool Supervision,
    bool Informe)
{
    // sumar cuidadores u observadores, y que el dueño deje de cuidar y solo mire
    public bool Grupo => Integrantes != 0;
}

public class CambiarPlanDto
{
    public string Plan { get; set; } = string.Empty;
}
