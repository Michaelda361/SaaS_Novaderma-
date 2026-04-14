# Skill: EF Core Migration — NovaHub

Flujo completo para crear y aplicar migraciones de Entity Framework Core.

## Cuándo crear una migración

Crear migración cuando se modifica cualquiera de estos archivos:
- `src/Domain/Entities/*.cs` — nueva propiedad, nuevo tipo, nueva entidad
- `src/Infrastructure/Persistence/AppDbContext.cs` — nuevo DbSet, nueva configuración de relación, nuevo query filter

No crear migración por cambios en DTOs, Services, Controllers o Client.

---

## Paso 1 — Verificar migraciones existentes

```bash
dotnet ef migrations list --project src/Infrastructure --startup-project src/Server
```

## Paso 2 — Crear la migración

```bash
dotnet ef migrations add <NombreMigracion> --project src/Infrastructure --startup-project src/Server
```

### Convención de nombres
- Nueva entidad: `Add{Entidad}` (ej: `AddProveedor`)
- Nueva propiedad: `Add{Propiedad}To{Entidad}` (ej: `AddTelefonoToColaborador`)
- Nueva relación: `Add{Relacion}` (ej: `AddColaboradorCertificadoRelation`)
- Cambio de tipo: `Change{Propiedad}TypeIn{Entidad}`

## Paso 3 — Revisar el archivo generado

Abrir `src/Infrastructure/Migrations/<timestamp>_<NombreMigracion>.cs` y verificar:
- El `Up()` hace exactamente lo esperado
- El `Down()` revierte correctamente
- No hay operaciones destructivas inesperadas (ej: `DropColumn` accidental)

## Paso 4 — Aplicar al DB

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Server
```

## Paso 5 — Verificar en AppDbContext

Si se agregó una nueva entidad, confirmar:
1. Existe `DbSet<{Entidad}>` en AppDbContext
2. Existe el global query filter si hereda BaseEntity:
   ```csharp
   modelBuilder.Entity<{Entidad}>().HasQueryFilter(e => e.Activo);
   ```
3. Relaciones con `OnDelete` configuradas si hay riesgo de cascade paths múltiples

---

## Problemas comunes

### Cascade delete conflict
SQL Server no permite múltiples cascade paths al mismo destino. Solución:
```csharp
modelBuilder.Entity<{Entidad}>()
    .HasOne(e => e.Relacionado)
    .WithMany()
    .HasForeignKey(e => e.RelacionadoId)
    .OnDelete(DeleteBehavior.Restrict); // o SetNull para opcionales
```

### Revertir última migración
```bash
# Revertir en DB primero
dotnet ef database update <MigracionAnterior> --project src/Infrastructure --startup-project src/Server
# Luego eliminar el archivo
dotnet ef migrations remove --project src/Infrastructure --startup-project src/Server
```

### Ver SQL generado sin aplicar
```bash
dotnet ef migrations script --project src/Infrastructure --startup-project src/Server
```

### Warning PendingModelChangesWarning
AppDbContext suprime este warning intencionalmente con ConfigureWarnings. Es normal y esperado.

### Entidades inmutables (sin soft delete)
Las entidades VersionDocumento, FlujoAprobacionDoc, PropuestaModificacion, AuditLog,
RespuestaCuestionario y RespuestaPregunta NO heredan BaseEntity y NO tienen global query filter.
No agregar HasQueryFilter para estas entidades.
