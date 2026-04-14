# Skill: Debugging y Troubleshooting — NovaHub

Guia para diagnosticar y resolver los problemas mas comunes del proyecto.

---

## Errores de compilacion frecuentes

### CS0246 — Tipo no encontrado
Causa: falta un using o una referencia de proyecto.
- Verificar que el namespace este en _Imports.razor (Client) o que el using este presente
- Verificar que el proyecto referenciado este en el .csproj

### Cascade delete conflict (EF migration)
Error: "Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths"
Solucion en AppDbContext:
  .HasOne(...).WithMany().HasForeignKey(...).OnDelete(DeleteBehavior.Restrict)

### PendingModelChangesWarning
AppDbContext suprime este warning intencionalmente. Es normal, no requiere accion.

---

## Problemas de autenticacion en desarrollo

### 401 Unauthorized en API
- Verificar que el servidor este corriendo con DevSettings configurado
- Verificar que el header X-Dev-User este siendo enviado por DevAwareAuthorizationMessageHandler
- Revisar appsettings.Development.json: DevSettings.DefaultDevUser debe tener un email valido

### AccessTokenNotAvailableException en Client
- En modo dev, capturar la excepcion y verificar isDevMode antes de llamar ex.Redirect()
- Patron correcto:
  catch (AccessTokenNotAvailableException ex) { if (!isDevMode) ex.Redirect(); }

---

## Problemas de EF Core

### Migracion pendiente al iniciar
Error al arrancar el servidor si hay migraciones sin aplicar.
Solucion: dotnet ef database update --project src/Infrastructure --startup-project src/Server

### Datos no aparecen en la UI
- Verificar que el global query filter no este excluyendo registros (Activo = false)
- Verificar que el Include() este cargando las navegaciones necesarias
- Usar .AsNoTracking() en queries de solo lectura

### Ciclo de serializacion JSON
Error: "A possible object cycle was detected"
Solucion: agregar [JsonIgnore] en las colecciones de navegacion inversa en las entidades Domain

---

## Problemas de SharePoint / Graph API

### En desarrollo: usar MockSharePointService
El mock se activa automaticamente cuando DevSettings.DefaultDevUser esta configurado.
No se necesita configuracion de Graph API para desarrollo local.

### En produccion: SharePointException
Si Graph API retorna error, SharePointService lanza SharePointException con el codigo y mensaje.
Verificar la seccion SharePoint en appsettings.json: TenantId, ClientId, ClientSecret, SiteUrl.

---

## Problemas de UI Blazor

### Toast no aparece
- Verificar que toast este declarado: private Toast toast = default!;
- Verificar que el componente este en el markup: <Toast @ref="toast" />
- Usar await toast.Show(...) — es async

### Modal no cierra despues de guardar
- Verificar que CerrarModal() se llame antes o despues de Cargar()
- Verificar que guardando = false se establezca en el bloque finally o al final

### Tabla vacia despues de crear/editar
- Verificar que Cargar() se llame despues de la operacion exitosa
- Verificar que el ApiService retorne el objeto creado (no null)

---

## Comandos utiles de diagnostico

# Ver estado de migraciones
dotnet ef migrations list --project src/Infrastructure --startup-project src/Server

# Build rapido sin restore
dotnet build --no-restore -v quiet

# Ver SQL de la ultima migracion
dotnet ef migrations script --project src/Infrastructure --startup-project src/Server

# Revertir ultima migracion
dotnet ef database update <MigracionAnterior> --project src/Infrastructure --startup-project src/Server
dotnet ef migrations remove --project src/Infrastructure --startup-project src/Server