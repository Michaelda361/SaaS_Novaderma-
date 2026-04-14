using Microsoft.EntityFrameworkCore;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suprimir warning de cambios pendientes — el modelo está sincronizado
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
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

    // Cartas Laborales
    public DbSet<PlantillaDocumento> PlantillasDocumento => Set<PlantillaDocumento>();
    public DbSet<PlantillaDocumentoArea> PlantillaDocumentoAreas => Set<PlantillaDocumentoArea>();
    public DbSet<SolicitudDocumento> SolicitudesDocumento => Set<SolicitudDocumento>();

    // Auditoría
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Cuestionarios
    public DbSet<Cuestionario> Cuestionarios => Set<Cuestionario>();
    public DbSet<Pregunta> Preguntas => Set<Pregunta>();
    public DbSet<OpcionRespuesta> OpcionesRespuesta => Set<OpcionRespuesta>();
    public DbSet<RespuestaCuestionario> RespuestasCuestionario => Set<RespuestaCuestionario>();
    public DbSet<RespuestaPregunta> RespuestasPregunta => Set<RespuestaPregunta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Precisión decimal
        modelBuilder.Entity<Inscripcion>()
            .Property(i => i.Calificacion)
            .HasPrecision(5, 2);

        // Soft delete filters — solo entidades que heredan BaseEntity (tienen columna Activo)
        modelBuilder.Entity<Colaborador>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Area>().HasQueryFilter(a => a.Activo);
        modelBuilder.Entity<Cargo>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Certificado>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Capacitacion>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Inscripcion>().HasQueryFilter(i => i.Activo);
        modelBuilder.Entity<RecursoCapacitacion>().HasQueryFilter(r => r.Activo);
        modelBuilder.Entity<Documento>().HasQueryFilter(d => d.Activo);

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

        // ── AuditLog ──────────────────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.Colaborador).WithMany()
            .HasForeignKey(a => a.ColaboradorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        // AuditLog no tiene soft delete — los registros nunca se eliminan

        // ── Cartas Laborales ──────────────────────────────────────────────────
        modelBuilder.Entity<PlantillaDocumento>()
            .HasQueryFilter(p => p.Activo);

        modelBuilder.Entity<PlantillaDocumento>()
            .Property(p => p.TipoPlantilla).HasConversion<string>();

        modelBuilder.Entity<SolicitudDocumento>()
            .Property(s => s.Estado).HasConversion<string>();

        // Query filters para evitar warnings de EF con relaciones requeridas
        modelBuilder.Entity<PlantillaDocumentoArea>()
            .HasQueryFilter(pa => pa.Area!.Activo && pa.PlantillaDocumento!.Activo);

        modelBuilder.Entity<SolicitudDocumento>()
            .HasQueryFilter(s => s.Activo);

        modelBuilder.Entity<PlantillaDocumentoArea>()
            .HasKey(pa => new { pa.PlantillaDocumentoId, pa.AreaId });

        modelBuilder.Entity<PlantillaDocumentoArea>()
            .HasOne(pa => pa.PlantillaDocumento)
            .WithMany(p => p.Areas)
            .HasForeignKey(pa => pa.PlantillaDocumentoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlantillaDocumentoArea>()
            .HasOne(pa => pa.Area)
            .WithMany()
            .HasForeignKey(pa => pa.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SolicitudDocumento>()
            .HasOne(s => s.PlantillaDocumento)
            .WithMany(p => p.Solicitudes)
            .HasForeignKey(s => s.PlantillaDocumentoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SolicitudDocumento>()
            .HasOne(s => s.Colaborador)
            .WithMany()
            .HasForeignKey(s => s.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Colaborador>()
            .Property(c => c.SueldoBasico)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Colaborador>()
            .Property(c => c.Rol).HasConversion<string>();

        // ── Índices ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Colaborador>().HasIndex(c => c.Email).HasFilter("[Activo] = 1");
        modelBuilder.Entity<Inscripcion>().HasIndex(i => i.CapacitacionId);
        modelBuilder.Entity<Inscripcion>().HasIndex(i => i.ColaboradorId);
        modelBuilder.Entity<RecursoCapacitacion>().HasIndex(r => r.CapacitacionId);
        modelBuilder.Entity<Certificado>().HasIndex(c => c.ColaboradorId);
        modelBuilder.Entity<SolicitudDocumento>().HasIndex(s => s.Estado).HasFilter("[Activo] = 1");
        modelBuilder.Entity<SolicitudDocumento>().HasIndex(s => s.ColaboradorId);
        modelBuilder.Entity<PropuestaModificacion>().HasIndex(p => p.AreaId);
        modelBuilder.Entity<PlantillaDocumentoArea>().HasIndex(pa => pa.AreaId);

        // ── Cuestionarios ─────────────────────────────────────────────────────
        modelBuilder.Entity<Cuestionario>().HasQueryFilter(c => c.Activo);
        modelBuilder.Entity<Pregunta>().HasQueryFilter(p => p.Activo);
        modelBuilder.Entity<OpcionRespuesta>().HasQueryFilter(o => o.Activo);

        modelBuilder.Entity<RespuestaCuestionario>()
            .Property(r => r.Puntaje).HasPrecision(5, 2);

        modelBuilder.Entity<Cuestionario>()
            .HasOne(c => c.Capacitacion).WithMany()
            .HasForeignKey(c => c.CapacitacionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pregunta>()
            .HasOne(p => p.Cuestionario).WithMany(c => c.Preguntas)
            .HasForeignKey(p => p.CuestionarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OpcionRespuesta>()
            .HasOne(o => o.Pregunta).WithMany(p => p.Opciones)
            .HasForeignKey(o => o.PreguntaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RespuestaCuestionario>()
            .HasOne(r => r.Inscripcion).WithMany()
            .HasForeignKey(r => r.InscripcionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RespuestaCuestionario>()
            .HasOne(r => r.Cuestionario).WithMany(c => c.Respuestas)
            .HasForeignKey(r => r.CuestionarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RespuestaPregunta>()
            .HasOne(r => r.RespuestaCuestionario).WithMany(rc => rc.Respuestas)
            .HasForeignKey(r => r.RespuestaCuestionarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RespuestaPregunta>()
            .HasOne(r => r.Pregunta).WithMany()
            .HasForeignKey(r => r.PreguntaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RespuestaPregunta>()
            .HasOne(r => r.OpcionElegida).WithMany()
            .HasForeignKey(r => r.OpcionElegidaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
