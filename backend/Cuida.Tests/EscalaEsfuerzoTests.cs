using Cuida.Api.Services;

namespace Cuida.Tests;

public class EscalaEsfuerzoTests
{
    private static bool[] ConSi(params int[] indices) =>
        Enumerable.Range(0, EscalaEsfuerzo.Preguntas.Length).Select(indices.Contains).ToArray();

    [Fact]
    public void Tiene_las_13_preguntas_del_indice()
    {
        Assert.Equal(13, EscalaEsfuerzo.Preguntas.Length);
    }

    [Fact]
    public void Seis_respuestas_afirmativas_no_llegan_al_punto_de_corte()
    {
        var resultado = EscalaEsfuerzo.Evaluar(ConSi(0, 1, 2, 3, 4, 5));

        Assert.Equal(6, resultado.Puntaje);
        Assert.False(resultado.Elevado);
    }

    [Fact]
    public void Siete_respuestas_afirmativas_indican_esfuerzo_elevado()
    {
        var resultado = EscalaEsfuerzo.Evaluar(ConSi(0, 1, 2, 3, 4, 5, 6));

        Assert.Equal(7, resultado.Puntaje);
        Assert.True(resultado.Elevado);
        Assert.Equal("Nivel elevado de esfuerzo", resultado.Nivel);
    }

    [Fact]
    public void Las_sugerencias_salen_de_las_areas_marcadas_y_no_se_repiten()
    {
        var economico = Array.FindIndex(EscalaEsfuerzo.Preguntas, p => p.Codigo == "economico");
        var restriccion = Array.FindIndex(EscalaEsfuerzo.Preguntas, p => p.Codigo == "restriccion");
        var planes = Array.FindIndex(EscalaEsfuerzo.Preguntas, p => p.Codigo == "planes");

        var resultado = EscalaEsfuerzo.Evaluar(ConSi(economico, restriccion, planes));

        // restricción y planes llevan a la misma sugerencia: aparece una sola vez
        Assert.Equal(new[] { "Tiempo propio", "Lo económico" }, resultado.Sugerencias.Select(s => s.Titulo));
    }

    [Fact]
    public void Sin_respuestas_afirmativas_no_hay_sugerencias()
    {
        var resultado = EscalaEsfuerzo.Evaluar(ConSi());

        Assert.Equal(0, resultado.Puntaje);
        Assert.Empty(resultado.Sugerencias);
    }
}
