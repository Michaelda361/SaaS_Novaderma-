using Microsoft.EntityFrameworkCore;
using TalentManagement.Domain.Entities;

namespace TalentManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Certificado> Certificados => Set<Certificado>();
    public DbSet<Capacitacion> Capacitaciones => Set<Capacitacion>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();
    public DbSet<RutaAprendizaje> RutasAprendizaje => Set<RutaAprendizaje>();
    public DbSet<RutaCapacitacion> RutaCapacitaciones => Set<RutaCapacitacion>();
    public DbSet<RecursoCapacitacion> RecursosCapacitacion => Set<RecursoCapacitacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Precisión decimal
        modelBuilder.Entity<Inscripcion>()
            .Property(i => i.Calificacion)
            .HasPrecision(5, 2);

        // Soft delete filters
        modelBuilder.Entity<Colaborador>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Area>().HasQueryFilter(a => a.Activo);
        modelBuilder.Entity<Cargo>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Certificado>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Capacitacion>().HasQueryFilter(c => c.Activo);

        // Evitar múltiples cascade paths en Colaborador
        modelBuilder.Entity<Colaborador>()
            .HasOne(c => c.Area)
            .WithMany(a => a.Colaboradores)
            .HasForeignKey(c => c.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Colaborador>()
            .HasOne(c => c.Cargo)
            .WithMany(ca => ca.Colaboradores)
            .HasForeignKey(c => c.CargoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auto-referencia supervisor
        modelBuilder.Entity<Colaborador>()
            .HasOne(c => c.Supervisor)
            .WithMany()
            .HasForeignKey(c => c.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Jefe de área — sin cascade para evitar ciclos
        modelBuilder.Entity<Area>()
            .HasOne(a => a.Jefe)
            .WithMany()
            .HasForeignKey(a => a.JefeId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
