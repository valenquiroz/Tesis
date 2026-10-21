using Cuida.Api.Models;
using Cuida.Api.Services;

namespace Cuida.Tests;

public class GuiaTramitesTests
{
    private static readonly DateTime Hoy = new(2026, 10, 12);

    private static string[] Codigos(PersonaAsistida persona) =>
        GuiaTramites.ParaPersona(persona, Hoy).Select(p => p.Codigo).ToArray();

    [Theory]
    [InlineData("Motora", "estudios-motora")]
    [InlineData("Visual", "estudios-visual")]
    [InlineData("Auditiva", "estudios-auditiva")]
    [InlineData("Visceral", "estudios-visceral")]
    [InlineData("Intelectual", "estudios-mental")]
    [InlineData("Psicosocial", "estudios-mental")]
    [InlineData("Múltiple", "estudios-multiple")]
    public void Cada_tipo_ve_solo_sus_estudios(string tipo, string esperado)
    {
        var estudios = Codigos(new PersonaAsistida { TipoDiscapacidad = tipo })
            .Where(c => c.StartsWith("estudios-"))
            .ToArray();

        Assert.Equal(new[] { esperado }, estudios);
    }

    [Fact]
    public void Sin_tipo_de_discapacidad_no_hay_estudios_pero_si_lo_general()
    {
        var codigos = Codigos(new PersonaAsistida());

        Assert.DoesNotContain(codigos, c => c.StartsWith("estudios-"));
        Assert.Contains("cud-consulta", codigos);
        Assert.Contains("cobertura-100", codigos);
    }

    [Theory]
    [InlineData("Hijo", true)]
    [InlineData("hija", true)]
    [InlineData("Madre", false)]
    [InlineData(null, false)]
    public void La_asignacion_por_hijo_aparece_solo_si_es_hijo_o_hija(string? relacion, bool aparece)
    {
        var codigos = Codigos(new PersonaAsistida { RelacionConCuidador = relacion });

        Assert.Equal(aparece, codigos.Contains("asignacion-hijo"));
    }

    [Fact]
    public void Los_estudios_para_bebes_no_se_muestran_a_adultos()
    {
        var adulto = new PersonaAsistida { TipoDiscapacidad = "Auditiva", FechaNacimiento = new DateTime(1990, 2, 14) };
        var bebe = new PersonaAsistida { TipoDiscapacidad = "Auditiva", FechaNacimiento = Hoy.AddYears(-2) };

        var paraAdulto = GuiaTramites.ParaPersona(adulto, Hoy).Single(p => p.Codigo == "estudios-auditiva");
        var paraBebe = GuiaTramites.ParaPersona(bebe, Hoy).Single(p => p.Codigo == "estudios-auditiva");

        Assert.DoesNotContain(paraAdulto.Documentacion, d => d.Texto.Contains("menores de 3"));
        Assert.Contains(paraBebe.Documentacion, d => d.Texto.Contains("menores de 3"));
    }

    [Fact]
    public void Todos_los_pasos_tienen_fuente_y_enlace_oficial()
    {
        foreach (var paso in GuiaTramites.Pasos)
        {
            Assert.False(string.IsNullOrWhiteSpace(paso.Fuente), paso.Codigo);
            Assert.StartsWith("https://", paso.Enlace);
        }
    }

    [Fact]
    public void Los_codigos_no_se_repiten()
    {
        var codigos = GuiaTramites.Pasos.Select(p => p.Codigo).ToList();

        Assert.Equal(codigos.Count, codigos.Distinct().Count());
    }
}
