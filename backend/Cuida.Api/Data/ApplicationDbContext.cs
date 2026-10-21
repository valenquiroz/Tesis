using Cuida.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cuida.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PersonaAsistida> PersonasAsistidas { get; set; }
    public DbSet<Turno> Turnos { get; set; }
    public DbSet<Documento> Documentos { get; set; }
    public DbSet<ContactoEmergencia> ContactosEmergencia { get; set; }
    public DbSet<RecursoRecomendable> RecursosRecomendables { get; set; }
    public DbSet<ArticuloRecomendado> ArticulosRecomendados { get; set; }
    public DbSet<TramitePersona> TramitesPersona { get; set; }
    public DbSet<PersonaCompartida> PersonasCompartidas { get; set; }
    public DbSet<Medicamento> Medicamentos { get; set; }
    public DbSet<TomaMedicamento> TomasMedicamentos { get; set; }
    public DbSet<EntradaDiario> EntradasDiario { get; set; }
    public DbSet<EvaluacionEsfuerzo> EvaluacionesEsfuerzo { get; set; }
    public DbSet<ValoracionRecurso> ValoracionesRecursos { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(u => u.Plan)
            .HasMaxLength(20)
            .HasDefaultValue(Planes.Gratis);

        builder.Entity<ApplicationUser>()
            .Property(u => u.NombreGrupo)
            .HasMaxLength(150);

        // con WithMany() vacío EF creaba una FK extra (ApplicationUserId)
        builder.Entity<PersonaAsistida>()
            .HasOne(p => p.Responsable)
            .WithMany(u => u.PersonasAsistidas)
            .HasForeignKey(p => p.ResponsableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PersonaAsistida>()
            .Property(p => p.ResponsableCuida)
            .HasDefaultValue(true);

        builder.Entity<Turno>()
            .HasOne(t => t.Cuidador)
            .WithMany()
            .HasForeignKey(t => t.CuidadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Turno>()
            .HasOne(t => t.PersonaAsistida)
            .WithMany()
            .HasForeignKey(t => t.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Documento>()
            .HasOne(d => d.Cuidador)
            .WithMany()
            .HasForeignKey(d => d.CuidadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Documento>()
            .HasOne(d => d.PersonaAsistida)
            .WithMany()
            .HasForeignKey(d => d.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ContactoEmergencia>()
            .HasOne(c => c.Cuidador)
            .WithMany()
            .HasForeignKey(c => c.CuidadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ContactoEmergencia>()
            .HasOne(c => c.PersonaAsistida)
            .WithMany()
            .HasForeignKey(c => c.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Trámites: un avance por persona y trámite de la guía
        builder.Entity<TramitePersona>()
            .HasOne(t => t.PersonaAsistida)
            .WithMany()
            .HasForeignKey(t => t.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TramitePersona>()
            .HasIndex(t => new { t.PersonaAsistidaId, t.Codigo })
            .IsUnique();

        // Valoraciones de recursos: una por persona y recurso
        builder.Entity<ValoracionRecurso>()
            .HasOne(v => v.PersonaAsistida)
            .WithMany()
            .HasForeignKey(v => v.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ValoracionRecurso>()
            .HasOne(v => v.RecursoRecomendable)
            .WithMany()
            .HasForeignKey(v => v.RecursoRecomendableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ValoracionRecurso>()
            .HasIndex(v => new { v.PersonaAsistidaId, v.RecursoRecomendableId })
            .IsUnique();

        // Equipo de cuidado de cada persona
        builder.Entity<PersonaCompartida>()
            .HasKey(c => new { c.PersonaAsistidaId, c.UsuarioId });

        builder.Entity<PersonaCompartida>()
            .Property(c => c.Rol)
            .HasDefaultValue(NivelesAcceso.Cuidador);

        builder.Entity<PersonaCompartida>()
            .HasOne(c => c.PersonaAsistida)
            .WithMany()
            .HasForeignKey(c => c.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PersonaCompartida>()
            .HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Medicación
        builder.Entity<Medicamento>()
            .HasOne(m => m.PersonaAsistida)
            .WithMany()
            .HasForeignKey(m => m.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TomaMedicamento>()
            .HasOne(t => t.Medicamento)
            .WithMany(m => m.Tomas)
            .HasForeignKey(t => t.MedicamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TomaMedicamento>()
            .HasOne(t => t.RegistradoPor)
            .WithMany()
            .HasForeignKey(t => t.RegistradoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TomaMedicamento>()
            .HasIndex(t => new { t.MedicamentoId, t.Fecha, t.Horario })
            .IsUnique();

        // Diario
        builder.Entity<EntradaDiario>()
            .HasOne(e => e.PersonaAsistida)
            .WithMany()
            .HasForeignKey(e => e.PersonaAsistidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EntradaDiario>()
            .HasOne(e => e.Autor)
            .WithMany()
            .HasForeignKey(e => e.AutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EntradaDiario>()
            .HasIndex(e => new { e.PersonaAsistidaId, e.Fecha });

        // Bienestar del cuidador
        builder.Entity<EvaluacionEsfuerzo>()
            .HasOne(e => e.Usuario)
            .WithMany()
            .HasForeignKey(e => e.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
