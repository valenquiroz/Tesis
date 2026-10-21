using Cuida.Api.Controllers;
using Cuida.Api.Models;
using Cuida.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cuida.Tests;

// Grupo de cuidado (dueño, cuidador, observador) y límites de los planes
public class EquipoYPlanesTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    public void Dispose() => _base.Dispose();

    private async Task<Medicamento> MedicamentoDeElenaAsync()
    {
        var contexto = _base.NuevoContexto();
        var medicamento = new Medicamento { PersonaAsistidaId = _base.Elena.Id, Nombre = "Levotiroxina", Horarios = "07:00" };
        contexto.Medicamentos.Add(medicamento);
        await contexto.SaveChangesAsync();
        return medicamento;
    }

    private async Task CambiarAsync(Action<Cuida.Api.Data.ApplicationDbContext> cambio)
    {
        var contexto = _base.NuevoContexto();
        cambio(contexto);
        await contexto.SaveChangesAsync();
    }

    private static Task<IActionResult> RegistrarTomaAsync(MedicamentosController controlador, int medicamentoId) =>
        controlador.RegistrarToma(medicamentoId,
            new RegistrarTomaDto { Fecha = DateTime.Today, Horario = "07:00", Estado = EstadosToma.Tomada });

    [Fact]
    public async Task Un_observador_ve_todo_pero_no_registra_tomas()
    {
        await CambiarAsync(c => c.PersonasCompartidas.Single(x => x.UsuarioId == _base.Lorena.Id).Rol = NivelesAcceso.Observador);
        var medicamento = await MedicamentoDeElenaAsync();

        var controlador = BaseDePrueba.Como(new MedicamentosController(_base.NuevoContexto()), _base.Lorena);
        var resultado = await RegistrarTomaAsync(controlador, medicamento.Id);

        Assert.True(await _base.NuevoContexto().PuedeVerAsync(_base.Lorena.Id, _base.Elena.Id));
        Assert.Equal(403, Assert.IsType<ObjectResult>(resultado).StatusCode);
        Assert.False(await _base.NuevoContexto().TomasMedicamentos.AnyAsync());
    }

    [Fact]
    public async Task El_responsable_en_modo_observador_administra_pero_no_registra()
    {
        await CambiarAsync(c => c.PersonasAsistidas.Single(p => p.Id == _base.Elena.Id).ResponsableCuida = false);
        var medicamento = await MedicamentoDeElenaAsync();
        var contexto = _base.NuevoContexto();

        var controlador = BaseDePrueba.Como(new MedicamentosController(_base.NuevoContexto()), _base.Jorge);
        var resultado = await RegistrarTomaAsync(controlador, medicamento.Id);

        Assert.Equal(403, Assert.IsType<ObjectResult>(resultado).StatusCode);
        Assert.True(await contexto.PuedeGestionarAsync(_base.Jorge.Id, _base.Elena.Id));
        Assert.True(await contexto.PersonasQueAdministra(_base.Jorge.Id).AnyAsync(p => p.Id == _base.Elena.Id));
    }

    [Fact]
    public async Task Un_cuidador_registra_pero_no_administra()
    {
        var contexto = _base.NuevoContexto();

        Assert.True(await contexto.PuedeRegistrarAsync(_base.Lorena.Id, _base.Elena.Id));
        Assert.False(await contexto.PersonasQueAdministra(_base.Lorena.Id).AnyAsync(p => p.Id == _base.Elena.Id));
    }

    [Fact]
    public async Task El_acceso_que_ve_la_interfaz_coincide_con_el_rol()
    {
        await CambiarAsync(c => c.PersonasCompartidas.Single(x => x.UsuarioId == _base.Lorena.Id).Rol = NivelesAcceso.Observador);
        var contexto = _base.NuevoContexto();
        var elena = await contexto.PersonasAsistidas.AsNoTracking().SingleAsync(p => p.Id == _base.Elena.Id);

        await contexto.CompletarAccesoAsync(_base.Lorena.Id, new[] { elena });

        Assert.Equal(NivelesAcceso.Observador, elena.Acceso);
        Assert.False(elena.PuedeRegistrar);
        Assert.False(elena.PuedeGestionar);
        Assert.False(elena.PuedeAdministrar);
        Assert.Equal("Jorge Gómez", elena.NombreTitular);
    }

    [Fact]
    public async Task El_dueno_ve_solo_las_personas_de_su_grupo()
    {
        var (dueno, _, paciente, _) = await CrearGrupoAsync();
        var contexto = _base.NuevoContexto();

        Assert.True(await contexto.PuedeVerAsync(dueno.Id, paciente.Id));
        Assert.False(await contexto.PuedeVerAsync(dueno.Id, _base.Rosa.Id));
        Assert.False(await contexto.PuedeRegistrarAsync(dueno.Id, paciente.Id));
    }

    [Fact]
    public async Task Un_cuidador_del_grupo_ve_solo_las_personas_que_tiene_asignadas()
    {
        var (_, cuidador, asignado, otro) = await CrearGrupoAsync();
        var contexto = _base.NuevoContexto();
        var persona = await contexto.PersonasAsistidas.AsNoTracking().SingleAsync(p => p.Id == asignado.Id);

        await contexto.CompletarAccesoAsync(cuidador.Id, new[] { persona });

        Assert.True(await contexto.PuedeRegistrarAsync(cuidador.Id, asignado.Id));
        Assert.False(await contexto.PuedeVerAsync(cuidador.Id, otro.Id));
        Assert.Equal("Residencia de prueba", persona.NombreTitular);

        // la dueña ve a cargo de quién está cada persona
        var deLaDuena = await contexto.PersonasAsistidas.AsNoTracking().SingleAsync(p => p.Id == asignado.Id);
        await contexto.CompletarAccesoAsync("dueno", new[] { deLaDuena });
        Assert.Equal(new[] { "Cuidador" }, deLaDuena.Cuidadores);

        // su plan es el del dueño del grupo
        var grupo = Assert.Single(await contexto.GruposDondeAyudaAsync(cuidador.Id));
        Assert.Equal(Planes.Plus, grupo.Plan);
        Assert.Equal("Residencia de prueba", grupo.Nombre);
    }

    [Fact]
    public async Task El_dueno_ve_lo_que_registro_cada_uno_y_con_plus_tambien_el_cumplimiento()
    {
        var (dueno, cuidador, asignado, _) = await CrearGrupoAsync();
        var contexto = _base.NuevoContexto();
        var medicamento = new Medicamento
        {
            PersonaAsistidaId = asignado.Id, Nombre = "Enalapril", Horarios = "00:00", FechaCreacion = DateTime.UtcNow.AddDays(-3),
        };
        contexto.Medicamentos.Add(medicamento);
        await contexto.SaveChangesAsync();
        contexto.TomasMedicamentos.Add(new TomaMedicamento
        {
            MedicamentoId = medicamento.Id, Fecha = DateTime.Today, Horario = "00:00", Estado = EstadosToma.Tomada,
            RegistradoPorId = cuidador.Id, FechaRegistro = DateTime.UtcNow,
        });
        await contexto.SaveChangesAsync();

        var conPlus = Json(await BaseDePrueba.Como(new GrupoController(_base.NuevoContexto(), null!), dueno).GetPersona(asignado.Id));
        await CambiarAsync(c => c.Users.Single(u => u.Id == dueno.Id).Plan = Planes.Equipo);
        var conEquipo = Json(await BaseDePrueba.Como(new GrupoController(_base.NuevoContexto(), null!), dueno).GetPersona(asignado.Id));

        // lo registrado se ve en los dos planes, con su autor
        Assert.Equal("Cuidador", conEquipo.GetProperty("Registros")[0].GetProperty("Autor").GetString());
        Assert.True(conEquipo.GetProperty("SinRegistrar").GetArrayLength() > 0);
        // el cumplimiento de cada cuidador, solo con Plus
        Assert.Equal(JsonValueKind.Null, conEquipo.GetProperty("PorCuidador").ValueKind);
        Assert.Equal(1, conPlus.GetProperty("PorCuidador")[0].GetProperty("Registradas").GetInt32());
    }

    [Fact]
    public async Task El_dueno_cambia_a_quienes_acompana_un_cuidador()
    {
        var (dueno, cuidador, asignado, otro) = await CrearGrupoAsync();
        var controlador = BaseDePrueba.Como(new GrupoController(_base.NuevoContexto(), null!), dueno);

        var resultado = await controlador.CambiarAsignacion(cuidador.Id, new AsignacionDto { Rol = NivelesAcceso.Cuidador, PersonasIds = { otro.Id } });
        var contexto = _base.NuevoContexto();

        Assert.IsType<OkObjectResult>(resultado);
        Assert.False(await contexto.PuedeVerAsync(cuidador.Id, asignado.Id));
        Assert.True(await contexto.PuedeRegistrarAsync(cuidador.Id, otro.Id));
    }

    [Fact]
    public async Task Nadie_ve_el_grupo_de_otro_dueno()
    {
        var (_, _, asignado, _) = await CrearGrupoAsync();
        await CambiarAsync(c => c.Users.Single(u => u.Id == _base.Jorge.Id).Plan = Planes.Plus);

        var resultado = await BaseDePrueba.Como(new GrupoController(_base.NuevoContexto(), null!), _base.Jorge).GetPersona(asignado.Id);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    private static JsonElement Json(IActionResult resultado) =>
        JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(resultado).Value);

    [Fact]
    public async Task El_plan_gratis_incluye_una_sola_persona()
    {
        var controlador = BaseDePrueba.Como(new PersonasAsistidasController(_base.NuevoContexto(), null!, null!), _base.Graciela);

        var resultado = await controlador.Crear(new PersonaAsistidaRequest { NombreCompleto = "Otra persona" });

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Equal(1, await _base.NuevoContexto().PersonasAsistidas.CountAsync(p => p.ResponsableId == _base.Graciela.Id));
    }

    [Fact]
    public async Task Con_el_plan_equipo_se_puede_sumar_otra_persona()
    {
        await CambiarAsync(c => c.Users.Single(u => u.Id == _base.Graciela.Id).Plan = Planes.Equipo);
        var controlador = BaseDePrueba.Como(new PersonasAsistidasController(_base.NuevoContexto(), null!, null!), _base.Graciela);

        var resultado = await controlador.Crear(new PersonaAsistidaRequest { NombreCompleto = "Otra persona" });

        Assert.IsType<CreatedAtActionResult>(resultado);
    }

    [Fact]
    public async Task El_diario_no_esta_en_el_plan_gratis()
    {
        var controlador = BaseDePrueba.Como(new DiarioController(_base.NuevoContexto()), _base.Graciela);

        var resultado = await controlador.GetEntradas(_base.Rosa.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task Los_tramites_no_estan_en_el_plan_gratis()
    {
        var gratis = await BaseDePrueba.Como(new TramitesController(_base.NuevoContexto()), _base.Graciela).GetHojaDeRuta(_base.Rosa.Id);
        await CambiarAsync(c => c.Users.Single(u => u.Id == _base.Graciela.Id).Plan = Planes.Equipo);
        var equipo = await BaseDePrueba.Como(new TramitesController(_base.NuevoContexto()), _base.Graciela).GetHojaDeRuta(_base.Rosa.Id);

        Assert.IsType<BadRequestObjectResult>(gratis);
        Assert.IsType<OkObjectResult>(equipo);
    }

    [Fact]
    public async Task Los_recursos_y_tu_bienestar_estan_desde_el_plan_equipo()
    {
        var recursosGratis = await BaseDePrueba.Como(new RecomendacionesController(_base.NuevoContexto(), null!), _base.Graciela).Obtener(_base.Rosa.Id);
        var bienestarGratis = await BaseDePrueba.Como(new BienestarController(_base.NuevoContexto()), _base.Graciela).Get();
        await CambiarAsync(c => c.Users.Single(u => u.Id == _base.Graciela.Id).Plan = Planes.Equipo);
        var bienestarEquipo = await BaseDePrueba.Como(new BienestarController(_base.NuevoContexto()), _base.Graciela).Get();

        Assert.IsType<BadRequestObjectResult>(recursosGratis);
        Assert.IsType<BadRequestObjectResult>(bienestarGratis);
        Assert.IsType<OkObjectResult>(bienestarEquipo);
    }

    [Fact]
    public async Task Los_articulos_son_del_plan_plus_y_cubren_a_quien_cuida_en_ese_grupo()
    {
        var (_, cuidador, _, _) = await CrearGrupoAsync();
        await CambiarAsync(c => c.Users.Single(u => u.Id == _base.Graciela.Id).Plan = Planes.Equipo);

        var conEquipo = await BaseDePrueba.Como(new ArticulosController(_base.NuevoContexto()), _base.Graciela).Listar();
        var cuidadorDeUnGrupoPlus = await BaseDePrueba.Como(new ArticulosController(_base.NuevoContexto()), cuidador).Listar();

        Assert.IsType<BadRequestObjectResult>(conEquipo);
        Assert.IsType<OkObjectResult>(cuidadorDeUnGrupoPlus);
    }

    [Fact]
    public async Task Un_observador_no_escribe_en_el_diario()
    {
        await CambiarAsync(c =>
        {
            c.Users.Single(u => u.Id == _base.Jorge.Id).Plan = Planes.Equipo;
            c.PersonasCompartidas.Single(x => x.UsuarioId == _base.Lorena.Id).Rol = NivelesAcceso.Observador;
        });
        var controlador = BaseDePrueba.Como(new DiarioController(_base.NuevoContexto()), _base.Lorena);

        var resultado = await controlador.Crear(new EntradaDiarioDto
        {
            PersonaAsistidaId = _base.Elena.Id, Fecha = DateTime.Today, Tipo = TiposDiario.Todos[0], Texto = "Durmió bien",
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.False(await _base.NuevoContexto().EntradasDiario.AnyAsync());
    }

    // un grupo (una residencia) con su dueña, un cuidador y dos personas (una asignada al cuidador)
    private async Task<(ApplicationUser Dueno, ApplicationUser Cuidador, PersonaAsistida Asignado, PersonaAsistida Otro)> CrearGrupoAsync()
    {
        var contexto = _base.NuevoContexto();

        var dueno = new ApplicationUser
        {
            Id = "dueno", UserName = "dueno@prueba.com", Email = "dueno@prueba.com", NombreCompleto = "Dueña",
            NombreGrupo = "Residencia de prueba", Plan = Planes.Plus,
        };
        var cuidador = new ApplicationUser
        {
            Id = "cuidador", UserName = "cuidador@prueba.com", Email = "cuidador@prueba.com", NombreCompleto = "Cuidador",
        };
        contexto.Users.AddRange(dueno, cuidador);

        var asignado = new PersonaAsistida { NombreCompleto = "Residente uno", ResponsableId = dueno.Id, ResponsableCuida = false };
        var otro = new PersonaAsistida { NombreCompleto = "Residente dos", ResponsableId = dueno.Id, ResponsableCuida = false };
        contexto.PersonasAsistidas.AddRange(asignado, otro);
        await contexto.SaveChangesAsync();

        contexto.PersonasCompartidas.Add(new PersonaCompartida { PersonaAsistidaId = asignado.Id, UsuarioId = cuidador.Id, Rol = NivelesAcceso.Cuidador });
        await contexto.SaveChangesAsync();

        return (dueno, cuidador, asignado, otro);
    }
}
