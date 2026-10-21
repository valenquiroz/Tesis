using Cuida.Api.Models;
using Cuida.Api.Services;

namespace Cuida.Tests;

public class RecomendacionServiceTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    public void Dispose() => _base.Dispose();

    private async Task<List<RecomendacionResultado>> RecomendarAsync(PersonaAsistida persona, params RecursoRecomendable[] recursos)
    {
        var contexto = _base.NuevoContexto();
        contexto.PersonasAsistidas.Add(persona);
        contexto.RecursosRecomendables.AddRange(recursos);
        await contexto.SaveChangesAsync();

        return await new RecomendacionService(_base.NuevoContexto()).ObtenerRecomendaciones(persona.Id);
    }

    private PersonaAsistida Persona() => new()
    {
        NombreCompleto = "Héctor Benítez",
        ResponsableId = _base.Graciela.Id,
        TipoDiscapacidad = "Visual",
        ObraSocial = "PAMI",
        Localidad = "Pilar",
    };

    // cada recurso es para uno o más tipos de discapacidad; por defecto, el de Héctor
    private static RecursoRecomendable Recurso(string titulo, string? tipo = "Visual", string? obraSocial = null, string? localidad = null) => new()
    {
        Titulo = titulo,
        Descripcion = "Descripción",
        Categoria = "Salud",
        TipoDiscapacidad = tipo,
        ObraSocial = obraSocial,
        Localidad = localidad,
    };

    [Fact]
    public async Task El_recurso_que_coincide_en_todo_queda_primero()
    {
        var lista = await RecomendarAsync(Persona(),
            Recurso("Visual", "Visual"),
            Recurso("Visual en Pilar con PAMI", "Visual", "PAMI", "Pilar"));

        Assert.Equal("Visual en Pilar con PAMI", lista[0].Recurso.Titulo);
        Assert.Equal("Visual", lista[1].Recurso.Titulo);
    }

    [Fact]
    public async Task Descarta_recursos_de_otra_localidad_o_discapacidad()
    {
        var lista = await RecomendarAsync(Persona(),
            Recurso("Para sordos", "Auditiva"),
            Recurso("En Rosario", localidad: "Rosario"),
            Recurso("Con IOMA", obraSocial: "IOMA"),
            Recurso("Visual", "Visual"));

        Assert.Equal(new[] { "Visual" }, lista.Select(r => r.Recurso.Titulo));
    }

    [Fact]
    public async Task No_distingue_mayusculas_ni_tildes()
    {
        var persona = Persona();
        persona.Localidad = "San Isidro";

        var lista = await RecomendarAsync(persona, Recurso("Sin tildes", localidad: "san isidro"));

        Assert.Single(lista);
    }

    [Fact]
    public async Task El_porcentaje_nunca_pasa_de_100()
    {
        var lista = await RecomendarAsync(Persona(), Recurso("Todo", "Visual", "PAMI", "Pilar"));

        Assert.InRange(lista.Single().Porcentaje, 1, 100);
    }

    [Fact]
    public async Task Solo_se_recomienda_lo_que_es_para_su_tipo_de_discapacidad()
    {
        var lista = await RecomendarAsync(Persona(),
            new RecursoRecomendable { Titulo = "Para cualquiera", Descripcion = "-", Categoria = "Salud" },
            Recurso("Motora y visual", "Motora, Visual"),
            Recurso("Solo motora", "Motora"));

        Assert.Equal(new[] { "Motora y visual" }, lista.Select(r => r.Recurso.Titulo));
    }

    [Fact]
    public async Task Lo_que_es_para_quien_no_tiene_obra_social_no_le_llega_a_quien_tiene_PAMI()
    {
        // defecto D10: Incluir Salud le aparecía a una persona con PAMI
        var conPami = await RecomendarAsync(Persona(), Recurso("Incluir Salud", obraSocial: "Sin obra social"));
        var sinObraSocial = Persona();
        sinObraSocial.ObraSocial = null;
        var conIncluirSalud = Persona();
        conIncluirSalud.ObraSocial = "Incluir Salud";
        var contexto = _base.NuevoContexto();
        contexto.PersonasAsistidas.AddRange(sinObraSocial, conIncluirSalud);
        await contexto.SaveChangesAsync();

        Assert.Empty(conPami);
        Assert.Single(await RecomendarAsync(sinObraSocial.Id));
        Assert.Single(await RecomendarAsync(conIncluirSalud.Id));
    }

    // --- valoraciones ("me sirvió" / "no me sirvió") ---

    private async Task<(PersonaAsistida Persona, RecursoRecomendable[] Recursos)> CargarAsync(params RecursoRecomendable[] recursos)
    {
        var persona = Persona();
        var contexto = _base.NuevoContexto();
        contexto.PersonasAsistidas.Add(persona);
        contexto.RecursosRecomendables.AddRange(recursos);
        await contexto.SaveChangesAsync();
        return (persona, recursos);
    }

    private async Task ValorarAsync(int personaId, int recursoId, bool util)
    {
        var contexto = _base.NuevoContexto();
        contexto.ValoracionesRecursos.Add(new ValoracionRecurso
        {
            PersonaAsistidaId = personaId,
            RecursoRecomendableId = recursoId,
            Util = util,
            Fecha = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync();
    }

    private async Task<PersonaAsistida> OtraPersonaAsync(string tipoDiscapacidad)
    {
        var otra = new PersonaAsistida { NombreCompleto = "Otra persona", ResponsableId = _base.Jorge.Id, TipoDiscapacidad = tipoDiscapacidad };
        var contexto = _base.NuevoContexto();
        contexto.PersonasAsistidas.Add(otra);
        await contexto.SaveChangesAsync();
        return otra;
    }

    private Task<List<RecomendacionResultado>> RecomendarAsync(int personaId) =>
        new RecomendacionService(_base.NuevoContexto()).ObtenerRecomendaciones(personaId);

    [Fact]
    public async Task Lo_que_te_sirvio_sube_sin_cambiar_el_porcentaje()
    {
        var (persona, recursos) = await CargarAsync(
            Recurso("Visual en Pilar", localidad: "Pilar"),
            Recurso("Solo visual"));
        var antes = await RecomendarAsync(persona.Id);
        Assert.Equal("Visual en Pilar", antes[0].Recurso.Titulo);

        await ValorarAsync(persona.Id, recursos[1].Id, util: true);
        var lista = await RecomendarAsync(persona.Id);

        Assert.Equal("Solo visual", lista[0].Recurso.Titulo);
        Assert.True(lista[0].Valoracion);
        Assert.Equal("Marcaste que te sirvió", lista[0].Motivos[0]);
        Assert.Equal(antes[1].Porcentaje, lista[0].Porcentaje);
    }

    [Fact]
    public async Task Lo_que_no_te_sirvio_queda_al_final_pero_sigue_en_la_lista()
    {
        var (persona, recursos) = await CargarAsync(
            Recurso("Visual en Pilar con PAMI", "Visual", "PAMI", "Pilar"),
            Recurso("General"));

        await ValorarAsync(persona.Id, recursos[0].Id, util: false);
        var lista = await RecomendarAsync(persona.Id);

        Assert.Equal(new[] { "General", "Visual en Pilar con PAMI" }, lista.Select(r => r.Recurso.Titulo));
        Assert.False(lista[1].Valoracion);
    }

    [Fact]
    public async Task Lo_que_les_sirvio_a_otros_con_la_misma_discapacidad_sube()
    {
        var (persona, recursos) = await CargarAsync(Recurso("A"), Recurso("B"));
        for (var i = 0; i < 3; i++)
            await ValorarAsync((await OtraPersonaAsync("Visual")).Id, recursos[1].Id, util: true);

        var lista = await RecomendarAsync(persona.Id);

        Assert.Equal("B", lista[0].Recurso.Titulo);
        Assert.Equal(3, lista[0].VotosUtiles);
        Assert.Contains("A 3 personas que cuidan a alguien con discapacidad visual les sirvió", lista[0].Motivos);
        Assert.Null(lista[0].Valoracion);
    }

    [Fact]
    public async Task No_cuentan_los_votos_de_otra_discapacidad()
    {
        var (persona, recursos) = await CargarAsync(Recurso("A"), Recurso("B"));
        await ValorarAsync(_base.Rosa.Id, recursos[1].Id, util: true); // Rosa tiene discapacidad motora

        var lista = await RecomendarAsync(persona.Id);

        Assert.Equal("A", lista[0].Recurso.Titulo);
        Assert.All(lista, r => Assert.Equal(0, r.VotosUtiles));
    }
}
