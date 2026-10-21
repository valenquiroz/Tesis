using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cuida.Api.Data;

// para dotnet ef: así no arranca Program.cs (no pide la clave JWT ni corre el seed)
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new ApplicationDbContext(opciones);
    }
}
