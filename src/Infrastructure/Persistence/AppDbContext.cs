using Microsoft.EntityFrameworkCore;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Certificado> Certificados => Set<Certificado>();
    public DbSet<Capacitacion> Capacitaciones => Set<Capacitacion>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();
    public DbSet<RecursoCapacitacion> RecursosCapacitacion => Set<RecursoCapacitacion>();

    // Control Documental
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<VersionDocumento> VersionesDocumento => Set<VersionDocumento>();
    public DbSet<PropuestaModificacion> PropuestasModificacion => Set<PropuestaModificacion>();
    public DbSet<FlujoAprobacionDoc> FlujosAprobacionDoc => Set<FlujoAprobacionDoc>();

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
        modelBuilder.Entity<Inscripcion>().HasQueryFilter(i => i.Activo);
        modelBuilder.Entity<RecursoCapacitacion>().HasQueryFilter(r => r.Activo);
        modelBuilder.Entity<Documento>().HasQueryFilter(d => d.Activo);

        // Filtros en entidades dependientes para evitar warnings de EF
        modelBuilder.Entity<Inscripcion>().HasQueryFilter(i => i.Capacitacion!.Activo);
        modelBuilder.Entity<RecursoCapacitacion>().HasQueryFilter(r => r.Capacitacion!.Activo);
        modelBuilder.Entity<FlujoAprobacionDoc>().HasQueryFilter(f => f.Colaborador!.Activo);
        modelBuilder.Entity<VersionDocumento>().HasQueryFilter(v => v.Documento!.Activo);
        modelBuilder.Entity<PropuestaModificacion>().HasQueryFilter(p => p.Area!.Activo);

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

        // ── Control Documental ────────────────────────────────────────────────

        // Enums como string para legibilidad en BD
        modelBuilder.Entity<Documento>()
            .Property(d => d.TipoDocumento).HasConversion<string>();
        modelBuilder.Entity<Documento>()
            .Property(d => d.Estado).HasConversion<string>();
        modelBuilder.Entity<PropuestaModificacion>()
            .Property(p => p.EstadoPropuesta).HasConversion<string>();
        modelBuilder.Entity<FlujoAprobacionDoc>()
            .Property(f => f.EstadoAnterior).HasConversion<string>();
        modelBuilder.Entity<FlujoAprobacionDoc>()
            .Property(f => f.EstadoNuevo).HasConversion<string>();

        // FK sin cascade para evitar ciclos con Colaborador
        modelBuilder.Entity<PropuestaModificacion>()
            .HasOne(p => p.Colaborador).WithMany()
            .HasForeignKey(p => p.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PropuestaModificacion>()
            .HasOne(p => p.Aprobador).WithMany()
            .HasForeignKey(p => p.AprobadorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<FlujoAprobacionDoc>()
            .HasOne(f => f.Colaborador).WithMany()
            .HasForeignKey(f => f.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
