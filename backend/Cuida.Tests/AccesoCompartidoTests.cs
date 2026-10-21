using Cuida.Api.Controllers;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Tests;

public class AccesoCompartidoTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    public async Task El_titular_ve_solo_sus_personas()
    {
        var nombres = await _base.Contexto.PersonasDe(_base.Graciela.Id).Select(p => p.NombreCompleto).ToListAsync();

        Assert.Equal(new[] { "Rosa Fernández" }, nombres);
    }

    [Fact]
    public async Task Un_colaborador_ve_la_persona_compartida()
    {
        Assert.True(await _base.Contexto.PuedeVerAsync(_base.Lorena.Id, _base.Elena.Id));
        Assert.False(await _base.Contexto.PuedeVerAsync(_base.Lorena.Id, _base.Rosa.Id));
    }

    [Fact]
    public async Task Nadie_ve_personas_ajenas()
    {
        Assert.False(await _base.Contexto.PuedeVerAsync(_base.Graciela.Id, _base.Elena.Id));
        Assert.Null(await _base.Contexto.PersonaDeAsync(_base.Jorge.Id, _base.Rosa.Id));
    }

    [Fact]
    public async Task La_guia_de_tramites_de_una_persona_ajena_da_404()
    {
        var controlador = BaseDePrueba.Como(new TramitesController(_base.NuevoContexto()), _base.Graciela);

        var resultado = await controlador.GetHojaDeRuta(_base.Elena.Id);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task Un_colaborador_puede_registrar_tomas_y_queda_quien_lo_hizo()
    {
        var contexto = _base.NuevoContexto();
        var medicamento = new Medicamento { PersonaAsistidaId = _base.Elena.Id, Nombre = "Levotiroxina", Horarios = "07:00" };
        contexto.Medicamentos.Add(medicamento);
        await contexto.SaveChangesAsync();

        var controlador = BaseDePrueba.Como(new MedicamentosController(_base.NuevoContexto()), _base.Lorena);
        var resultado = await controlador.RegistrarToma(medicamento.Id,
            new RegistrarTomaDto { Fecha = DateTime.Today, Horario = "07:00", Estado = EstadosToma.Tomada });

        Assert.IsType<OkObjectResult>(resultado);
        var toma = await _base.NuevoContexto().TomasMedicamentos.SingleAsync();
        Assert.Equal(_base.Lorena.Id, toma.RegistradoPorId);
    }

    [Fact]
    public async Task Alguien_ajeno_no_puede_tocar_la_medicacion()
    {
        var contexto = _base.NuevoContexto();
        var medicamento = new Medicamento { PersonaAsistidaId = _base.Elena.Id, Nombre = "Levotiroxina", Horarios = "07:00" };
        contexto.Medicamentos.Add(medicamento);
        await contexto.SaveChangesAsync();

        var controlador = BaseDePrueba.Como(new MedicamentosController(_base.NuevoContexto()), _base.Graciela);
        var resultado = await controlador.RegistrarToma(medicamento.Id,
            new RegistrarTomaDto { Fecha = DateTime.Today, Horario = "07:00", Estado = EstadosToma.Tomada });

        Assert.IsType<NotFoundObjectResult>(resultado);
        Assert.False(await _base.NuevoContexto().TomasMedicamentos.AnyAsync());
    }

    [Fact]
    public async Task No_se_registran_tomas_de_dias_futuros_ni_fuera_de_horario()
    {
        var contexto = _base.NuevoContexto();
        var medicamento = new Medicamento { PersonaAsistidaId = _base.Rosa.Id, Nombre = "Enalapril", Horarios = "08:00,20:00" };
        contexto.Medicamentos.Add(medicamento);
        await contexto.SaveChangesAsync();

        var controlador = BaseDePrueba.Como(new MedicamentosController(_base.NuevoContexto()), _base.Graciela);

        var futuro = await controlador.RegistrarToma(medicamento.Id,
            new RegistrarTomaDto { Fecha = DateTime.Today.AddDays(1), Horario = "08:00", Estado = EstadosToma.Tomada });
        var otroHorario = await controlador.RegistrarToma(medicamento.Id,
            new RegistrarTomaDto { Fecha = DateTime.Today, Horario = "09:00", Estado = EstadosToma.Tomada });

        Assert.IsType<BadRequestObjectResult>(futuro);
        Assert.IsType<BadRequestObjectResult>(otroHorario);
    }

    [Fact]
    public async Task Al_borrar_la_persona_se_borra_todo_lo_suyo()
    {
        var contexto = _base.NuevoContexto();
        var medicamento = new Medicamento { PersonaAsistidaId = _base.Elena.Id, Nombre = "Levotiroxina", Horarios = "07:00" };
        medicamento.Tomas.Add(new TomaMedicamento { Fecha = DateTime.Today, Horario = "07:00", RegistradoPorId = _base.Lorena.Id });
        contexto.Medicamentos.Add(medicamento);
        contexto.EntradasDiario.Add(new EntradaDiario { PersonaAsistidaId = _base.Elena.Id, AutorId = _base.Lorena.Id, Tipo = "Ánimo", Texto = "Estuvo triste", Fecha = DateTime.Today });
        await contexto.SaveChangesAsync();

        var borrar = _base.NuevoContexto();
        borrar.PersonasAsistidas.Remove(await borrar.PersonasAsistidas.SingleAsync(p => p.Id == _base.Elena.Id));
        await borrar.SaveChangesAsync();

        var despues = _base.NuevoContexto();
        Assert.False(await despues.Medicamentos.AnyAsync());
        Assert.False(await despues.TomasMedicamentos.AnyAsync());
        Assert.False(await despues.EntradasDiario.AnyAsync());
        Assert.False(await despues.PersonasCompartidas.AnyAsync());
    }
}
