---
inclusion: always
---

# Code Patterns — NovaHub

Patrones de código concretos usados en el proyecto. Seguirlos garantiza consistencia entre capas.

## 1. Entidad de Dominio

```csharp
// src/Domain/Entities/Ejemplo.cs
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Ejemplo : BaseEntity   // hereda Id (int) + Activo (bool = true)
{
    public string Nombre { get; set; } = string.Empty;
    public int OtraEntidadId { get; set; }
    public OtraEntidad OtraEntidad { get; set; } = null!;
    // Colecciones con [JsonIgnore] para evitar ciclos
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<Relacionado> Relacionados { get; set; } = [];
}
```

## 2. DTOs (Shared)

```csharp
// src/Shared/DTOs/Ejemplos/EjemploDto.cs
namespace TalentManagement.Shared.DTOs.Ejemplos;

public class EjemploDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int OtraEntidadId { get; set; }
    public string? OtraEntidadNombre { get; set; }
}

// CreateEjemploDto — con DataAnnotations
public class CreateEjemploDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La entidad es requerida")]
    public int OtraEntidadId { get; set; }
}

// UpdateEjemploDto — mismas propiedades que Create salvo excepciones
public class UpdateEjemploDto : CreateEjemploDto { }
```

## 3. Interfaz de Repositorio

```csharp
// src/Application/Interfaces/IEjemploRepository.cs
namespace TalentManagement.Application.Interfaces;

public interface IEjemploRepository
{
    Task<IEnumerable<Ejemplo>> GetAllAsync();
    Task<Ejemplo?> GetByIdAsync(int id);
    Task<Ejemplo> CreateAsync(Ejemplo entity);
    Task<Ejemplo> UpdateAsync(Ejemplo entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
```

## 4. Repositorio (Infrastructure)

```csharp
public class EjemploRepository(AppDbContext context) : IEjemploRepository
{
    public async Task<IEnumerable<Ejemplo>> GetAllAsync() =>
        await context.Ejemplos.Include(e => e.OtraEntidad).AsNoTracking().ToListAsync();

    public async Task<Ejemplo?> GetByIdAsync(int id) =>
        await context.Ejemplos.Include(e => e.OtraEntidad).FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Ejemplo> CreateAsync(Ejemplo entity)
    {
        context.Ejemplos.Add(entity);
        await context.SaveChangesAsync();
        return await GetByIdAsync(entity.Id) ?? entity;
    }

    public async Task<Ejemplo> UpdateAsync(Ejemplo entity)
    {
        context.Ejemplos.Update(entity);
        await context.SaveChangesAsync();
        return await GetByIdAsync(entity.Id) ?? entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await context.Ejemplos.FindAsync(id);
        if (entity is null) return;
        entity.Activo = false;   // SIEMPRE soft delete — nunca context.Remove()
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id) =>
        await context.Ejemplos.AnyAsync(e => e.Id == id);
}
```

## 5. Service (Application)

```csharp
public class EjemploService(IEjemploRepository repository)
{
    public async Task<List<EjemploDto>> GetAllAsync() =>
        (await repository.GetAllAsync()).Select(MapToDto).ToList();

    public async Task<EjemploDto?> GetByIdAsync(int id)
    {
        var entity = await repository.GetByIdAsync(id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<EjemploDto> CreateAsync(CreateEjemploDto dto)
    {
        var entity = new Ejemplo { Nombre = dto.Nombre, OtraEntidadId = dto.OtraEntidadId };
        return MapToDto(await repository.CreateAsync(entity));
    }

    public async Task<EjemploDto?> UpdateAsync(int id, UpdateEjemploDto dto)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity is null) return null;
        entity.Nombre = dto.Nombre;
        entity.OtraEntidadId = dto.OtraEntidadId;
        return MapToDto(await repository.UpdateAsync(entity));
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (await repository.GetByIdAsync(id) is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    private static EjemploDto MapToDto(Ejemplo e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        OtraEntidadId = e.OtraEntidadId,
        OtraEntidadNombre = e.OtraEntidad?.Nombre
    };
}
```

## 6. Controller (Server)

```csharp
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class EjemplosController(EjemploService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEjemploDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEjemploDto dto)
    {
        var result = await service.UpdateAsync(id, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await service.DeleteAsync(id) ? NoContent() : NotFound();
}
```

## 7. Client ApiService

```csharp
public class EjemploApiService(HttpClient http)
{
    private const string Base = "api/v1/ejemplos";

    public Task<List<EjemploDto>?> GetAllAsync() =>
        http.GetFromJsonAsync<List<EjemploDto>>(Base);

    public Task<EjemploDto?> GetByIdAsync(int id) =>
        http.GetFromJsonAsync<EjemploDto>($"{Base}/{id}");

    public async Task<EjemploDto?> CreateAsync(CreateEjemploDto dto)
    {
        var r = await http.PostAsJsonAsync(Base, dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<EjemploDto>() : null;
    }

    public async Task<EjemploDto?> UpdateAsync(int id, UpdateEjemploDto dto)
    {
        var r = await http.PutAsJsonAsync($"{Base}/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<EjemploDto>() : null;
    }

    public Task<bool> DeleteAsync(int id) =>
        http.DeleteAsync($"{Base}/{id}").ContinueWith(t => t.Result.IsSuccessStatusCode);
}
```

## 8. Página Blazor — estructura base

```razor
@page "/ejemplos"
@attribute [Authorize]
@inject EjemploApiService ApiService

<Toast @ref="toast" />
<ConfirmDialog Visible="confirmVisible"
               Message="¿Seguro que deseas eliminar este registro?"
               OnConfirm="ConfirmarEliminar"
               OnCancel="() => confirmVisible = false" />

<div class="card">
    <div class="card-header">
        <span class="card-title">Ejemplos</span>
        <button class="btn btn-primary btn-sm" @onclick="AbrirModal">+ Nuevo</button>
    </div>

    @if (cargando)
    {
        <div class="loading"><div class="spinner"></div> Cargando...</div>
    }
    else if (!items?.Any() ?? true)
    {
        <div class="empty-state">
            <div class="empty-state-icon">📋</div>
            <h3>Sin registros</h3>
            <p>Agrega el primero para comenzar.</p>
        </div>
    }
    else
    {
        <div class="table-wrapper">
            <table>
                <thead><tr><th>Nombre</th><th></th></tr></thead>
                <tbody>
                    @foreach (var item in items!)
                    {
                        <tr>
                            <td>@item.Nombre</td>
                            <td>
                                <div class="flex gap-2">
                                    <button class="btn btn-ghost btn-sm btn-icon" @onclick="() => AbrirEditar(item)">✏️</button>
                                    <button class="btn btn-danger btn-sm btn-icon" @onclick="() => PedirConfirmacion(item.Id)">🗑️</button>
                                </div>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>

@code {
    private List<EjemploDto>? items;
    private bool cargando = true, mostrarModal = false, guardando = false, confirmVisible = false;
    private int? editandoId;
    private int eliminandoId;
    private Toast toast = default!;
    private CreateEjemploDto form = new();

    protected override async Task OnInitializedAsync() => await Cargar();

    private async Task Cargar()
    {
        cargando = true;
        items = await ApiService.GetAllAsync();
        cargando = false;
    }

    private void AbrirModal() { form = new(); editandoId = null; mostrarModal = true; }
    private void AbrirEditar(EjemploDto item) { form = new() { Nombre = item.Nombre }; editandoId = item.Id; mostrarModal = true; }
    private void CerrarModal() => mostrarModal = false;

    private async Task Guardar()
    {
        guardando = true;
        bool ok = editandoId.HasValue
            ? await ApiService.UpdateAsync(editandoId.Value, new UpdateEjemploDto { Nombre = form.Nombre }) is not null
            : await ApiService.CreateAsync(form) is not null;
        guardando = false;
        if (ok) { CerrarModal(); await Cargar(); await toast.Show(editandoId.HasValue ? "Actualizado." : "Creado."); }
        else await toast.Show("Error al guardar.", "error");
    }

    private void PedirConfirmacion(int id) { eliminandoId = id; confirmVisible = true; }

    private async Task ConfirmarEliminar()
    {
        confirmVisible = false;
        if (await ApiService.DeleteAsync(eliminandoId)) { await Cargar(); await toast.Show("Eliminado."); }
        else await toast.Show("No se pudo eliminar.", "error");
    }
}
```

## 9. Toast — uso correcto

```csharp
await toast.Show("Operación exitosa.");           // success (default)
await toast.Show("Error al guardar.", "error");   // error
await toast.Show("Revisa los datos.", "warning"); // warning
```

## 10. Soft delete — regla absoluta

- Repositorio: `entity.Activo = false` + `SaveChangesAsync()` — NUNCA `context.Remove()`
- Global query filters en AppDbContext excluyen `Activo = false` automáticamente
- `.IgnoreQueryFilters()` solo en casos muy justificados (ej: panel de administración de inactivos)
