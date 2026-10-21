using System.Security.Claims;
using Cuida.Api.Data;
using Cuida.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Tests;

// Base SQLite en memoria: es relacional y respeta las claves foráneas,
// así que se parece más a SQL Server que el proveedor InMemory de EF.
public sealed class BaseDePrueba : IDisposable
{
    private readonly SqliteConnection _conexion;

    public ApplicationDbContext Contexto { get; }

    public ApplicationUser Graciela { get; }
    public ApplicationUser Jorge { get; }
    public ApplicationUser Lorena { get; }

    public PersonaAsistida Rosa { get; }
    public PersonaAsistida Elena { get; }

    public BaseDePrueba()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        Contexto = NuevoContexto();
        Contexto.Database.EnsureCreated();

        Graciela = Usuario("graciela", "Graciela Ibarra");
        Jorge = Usuario("jorge", "Jorge Gómez");
        Lorena = Usuario("lorena", "Lorena Gómez");

        Rosa = new PersonaAsistida { NombreCompleto = "Rosa Fernández", ResponsableId = Graciela.Id, TipoDiscapacidad = "Motora" };
        Elena = new PersonaAsistida { NombreCompleto = "Elena Gómez", ResponsableId = Jorge.Id, TipoDiscapacidad = "Auditiva" };
        Contexto.PersonasAsistidas.AddRange(Rosa, Elena);
        Contexto.SaveChanges();

        // Jorge comparte a Elena con su hermana
        Contexto.PersonasCompartidas.Add(new PersonaCompartida { PersonaAsistidaId = Elena.Id, UsuarioId = Lorena.Id });
        Contexto.SaveChanges();
        Contexto.ChangeTracker.Clear();
    }

    // un contexto nuevo sobre la misma base, para no leer desde la caché
    public ApplicationDbContext NuevoContexto() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conexion).Options);

    private ApplicationUser Usuario(string id, string nombre)
    {
        var usuario = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@prueba.com",
            Email = $"{id}@prueba.com",
            NombreCompleto = nombre,
        };
        Contexto.Users.Add(usuario);
        Contexto.SaveChanges();
        return usuario;
    }

    public static T Como<T>(T controlador, ApplicationUser usuario) where T : ControllerBase
    {
        controlador.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, usuario.Id), new Claim(ClaimTypes.Role, "Cuidador") },
                    "prueba")),
            },
        };
        return controlador;
    }

    public void Dispose()
    {
        Contexto.Dispose();
        _conexion.Dispose();
    }
}
