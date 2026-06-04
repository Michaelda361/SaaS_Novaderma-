using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Certificados;
using TalentManagement.Shared.DTOs.Cuestionarios;

namespace TalentManagement.Application.Services;

public class CuestionarioService(
    ICuestionarioRepository repository,
    IColaboradorRepository colaboradorRepository,
    ICapacitacionRepository capacitacionRepository,
    ICertificadoRepository certificadoRepository,
    IInscripcionRepository inscripcionRepository,
    IAuditLogRepository auditLogRepository,
    CertificadoService certificadoService,
    ICertificatePdfGenerator certificatePdfGenerator,
    ICertificadoPdfService certificadoPdfService)
{
    public async Task<CuestionarioDto?> GetByCapacitacionAsync(int capacitacionId)
    {
        var c = await repository.GetByCapacitacionAsync(capacitacionId);
        return c is null ? null : MapToDto(c);
    }

    /// <summary>Construye el DTO de notificación para enviar al Jefe via SignalR.</summary>
    public async Task<CuestionarioRespondidoDto?> BuildNotificacionAsync(
        int cuestionarioId, string emailColaborador, ResultadoCuestionarioDto resultado)
    {
        try
        {
            var cuestionario = await repository.GetByIdAsync(cuestionarioId);
            if (cuestionario is null) return null;

            var colaborador = await colaboradorRepository.GetByEmailAsync(emailColaborador);
            if (colaborador is null) return null;

            var capacitacion = await capacitacionRepository.GetByIdAsync(cuestionario.CapacitacionId);
            if (capacitacion is null) return null;

            return new CuestionarioRespondidoDto
            {
                ColaboradorNombre = $"{colaborador.Nombre} {colaborador.Apellido}",
                CapacitacionNombre = capacitacion.Nombre,
                CapacitacionId = capacitacion.Id,
                Puntaje = resultado.Puntaje,
                Aprobado = resultado.Aprobado,
                Correctas = resultado.Correctas,
                TotalPreguntas = resultado.TotalPreguntas,
            };
        }
        catch { return null; }
    }

    public async Task<CuestionarioDto> CreateAsync(CreateCuestionarioDto dto)
    {
        if (dto.IntentosPermitidos < 1)
        {
            throw new InvalidOperationException("La cantidad de intentos permitidos debe ser al menos 1.");
        }

        var cuestionario = new Cuestionario
        {
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            PuntajeAprobacion = dto.PuntajeAprobacion,
            AprobacionPorCorrectas = dto.AprobacionPorCorrectas,
            MinCorrectas = dto.MinCorrectas,
            IntentosPermitidos = dto.IntentosPermitidos,
            CapacitacionId = dto.CapacitacionId,
            Preguntas = dto.Preguntas.Select((p, pi) => new Pregunta
            {
                Enunciado = p.Enunciado,
                Orden = p.Orden > 0 ? p.Orden : pi + 1,
                Opciones = p.Opciones.Select((o, oi) => new OpcionRespuesta
                {
                    Texto = o.Texto,
                    EsCorrecta = o.EsCorrecta,
                    Orden = o.Orden > 0 ? o.Orden : oi + 1
                }).ToList()
            }).ToList()
        };

        var created = await repository.CreateAsync(cuestionario);
        return MapToDto(created);
    }

    public async Task<CuestionarioDto?> UpdateAsync(int id, CreateCuestionarioDto dto)
    {
        if (dto.IntentosPermitidos < 1)
        {
            throw new InvalidOperationException("La cantidad de intentos permitidos debe ser al menos 1.");
        }

        var existing = await repository.GetByIdAsync(id);
        if (existing is null) return null;

        existing.Titulo = dto.Titulo;
        existing.Descripcion = dto.Descripcion;
        existing.PuntajeAprobacion = dto.PuntajeAprobacion;
        existing.AprobacionPorCorrectas = dto.AprobacionPorCorrectas;
        existing.MinCorrectas = dto.MinCorrectas;
        existing.IntentosPermitidos = dto.IntentosPermitidos;

        // Reemplazar preguntas completas
        existing.Preguntas = dto.Preguntas.Select((p, pi) => new Pregunta
        {
            Enunciado = p.Enunciado,
            Orden = p.Orden > 0 ? p.Orden : pi + 1,
            CuestionarioId = id,
            Opciones = p.Opciones.Select((o, oi) => new OpcionRespuesta
            {
                Texto = o.Texto,
                EsCorrecta = o.EsCorrecta,
                Orden = o.Orden > 0 ? o.Orden : oi + 1
            }).ToList()
        }).ToList();

        var updated = await repository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var c = await repository.GetByIdAsync(id);
        if (c is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<ResultadoCuestionarioDto> ResponderAsync(ResponderCuestionarioDto dto)
    {
        if (dto is null)
            throw new InvalidOperationException("Payload de respuesta inválido.");

        dto.Respuestas ??= new();
        Console.WriteLine($"[DEBUG CuestionarioService.ResponderAsync] Inicio. CuestionarioId={dto.CuestionarioId}, InscripcionId={dto.InscripcionId}, Respuestas={dto.Respuestas.Count}");

        if (dto.CuestionarioId <= 0 || dto.InscripcionId <= 0)
            throw new InvalidOperationException("CuestionarioId e InscripcionId deben ser valores válidos.");

        var cuestionario = await repository.GetByIdAsync(dto.CuestionarioId)
            ?? throw new InvalidOperationException("Cuestionario no encontrado.");

        // Obtener respuestas existentes (intentos anteriores)
        var respuestas = await repository.GetRespuestasAsync(dto.CuestionarioId, dto.InscripcionId);
        var intentosRealizados = respuestas.Count;
        var aprobadoPrevio = respuestas.Any(r => r.Aprobado);

        if (aprobadoPrevio || intentosRealizados >= cuestionario.IntentosPermitidos)
        {
            Console.WriteLine($"[DEBUG CuestionarioService.ResponderAsync] Ya no tiene intentos disponibles o ya aprobó. Intentos: {intentosRealizados}/{cuestionario.IntentosPermitidos}, Aprobado={aprobadoPrevio}");
            var mejorRespuesta = respuestas.FirstOrDefault(r => r.Aprobado) 
                ?? respuestas.OrderByDescending(r => r.FechaRespuesta).First();

            return new ResultadoCuestionarioDto
            {
                Puntaje = mejorRespuesta.Puntaje,
                Aprobado = mejorRespuesta.Aprobado,
                PuntajeAprobacion = cuestionario.PuntajeAprobacion,
                AprobacionPorCorrectas = cuestionario.AprobacionPorCorrectas,
                MinCorrectas = cuestionario.MinCorrectas,
                TotalPreguntas = cuestionario.Preguntas.Count,
                Correctas = mejorRespuesta.TotalCorrectas,
                IntentosMaximos = cuestionario.IntentosPermitidos,
                IntentosRealizados = intentosRealizados,
                PuedeResponderOtroIntento = false,
                FechaFinalizacion = mejorRespuesta.FechaRespuesta
            };
        }

        int correctas = 0;
        var respuestasEntidad = new List<RespuestaPregunta>();

        foreach (var r in dto.Respuestas)
        {
            var pregunta = cuestionario.Preguntas.FirstOrDefault(p => p.Id == r.PreguntaId);
            if (pregunta is null) continue;

            var opcion = pregunta.Opciones.FirstOrDefault(o => o.Id == r.OpcionElegidaId);
            if (opcion?.EsCorrecta == true) correctas++;

            respuestasEntidad.Add(new RespuestaPregunta
            {
                PreguntaId = r.PreguntaId,
                OpcionElegidaId = r.OpcionElegidaId
            });
        }

        int total = cuestionario.Preguntas.Count;
        decimal puntaje = total > 0 ? Math.Round((decimal)correctas / total * 100, 2) : 0;
        bool aprobado;
        if (cuestionario.AprobacionPorCorrectas)
        {
            aprobado = correctas >= cuestionario.MinCorrectas;
        }
        else
        {
            aprobado = puntaje >= cuestionario.PuntajeAprobacion;
        }

        var respuesta = new RespuestaCuestionario
        {
            CuestionarioId = dto.CuestionarioId,
            InscripcionId = dto.InscripcionId,
            FechaRespuesta = DateTime.UtcNow,
            Puntaje = puntaje,
            Aprobado = aprobado,
            TotalCorrectas = correctas,
            Respuestas = respuestasEntidad
        };

        await repository.SaveRespuestaAsync(respuesta);
        
        var nuevosIntentosRealizados = intentosRealizados + 1;
        Console.WriteLine($"[DEBUG CuestionarioService.ResponderAsync] Respuesta guardada. Aprobado={aprobado}, Puntaje={puntaje:0.##}, Intento {nuevosIntentosRealizados}/{cuestionario.IntentosPermitidos}");

        bool certificadoEmitido = false;
        string? nombreCertificado = null;

        if (aprobado)
        {
            try
            {
                var inscripcion = await inscripcionRepository.GetByIdAsync(dto.InscripcionId);
                if (inscripcion is not null)
                {
                    var capacitacion = await capacitacionRepository.GetByIdAsync(cuestionario.CapacitacionId);
                    if (capacitacion is not null && capacitacion.EmiteCertificado)
                    {
                        var colEntity = await colaboradorRepository.GetByIdAsync(inscripcion.ColaboradorId);

                        string nombreCert;
                        if (!string.IsNullOrWhiteSpace(capacitacion.PlantillaNombreCertificado) && colEntity is not null)
                        {
                            nombreCert = capacitacion.PlantillaNombreCertificado
                                .Replace("{{nombre_completo}}", $"{colEntity.Nombre} {colEntity.Apellido}")
                                .Replace("{{cargo}}", colEntity.Cargo?.Nombre ?? "")
                                .Replace("{{area}}", colEntity.Area?.Nombre ?? "")
                                .Replace("{{capacitacion}}", capacitacion.Nombre)
                                .Replace("{{fecha_emision}}", DateTime.Today.ToString("dd/MM/yyyy"))
                                .Replace("{{puntaje}}", $"{puntaje:0.#}%");
                        }
                        else
                        {
                            nombreCert = !string.IsNullOrWhiteSpace(capacitacion.NombreCertificado)
                                ? capacitacion.NombreCertificado
                                : capacitacion.Nombre;
                        }

                        var existentes = await certificadoRepository.GetByColaboradorAsync(inscripcion.ColaboradorId);
                        var yaExiste = existentes.Any(c => c.CapacitacionId == capacitacion.Id);

                        if (!yaExiste)
                        {
                            byte[]? pdfBytes = null;
                            try
                            {
                                var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["{{nombre_completo}}"] = colEntity is not null ? $"{colEntity.Nombre} {colEntity.Apellido}" : string.Empty,
                                    ["{{cargo}}"] = colEntity?.Cargo?.Nombre ?? string.Empty,
                                    ["{{area}}"] = colEntity?.Area?.Nombre ?? string.Empty,
                                    ["{{capacitacion}}"] = capacitacion.Nombre,
                                    ["{{fecha_emision}}"] = DateTime.Today.ToString("dd/MM/yyyy"),
                                    ["{{puntaje}}"] = string.Empty
                                };

                                if (capacitacion.ArchivoDocxCertificado is { Length: > 0 })
                                {
                                    var mimeType = capacitacion.TipoArchivoCertificado
                                        ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

                                    pdfBytes = certificadoPdfService.GenerarPdf(capacitacion.ArchivoDocxCertificado, variables, mimeType);
                                }
                                else
                                {
                                    var pdfData = new CertificatePdfDataDto
                                    {
                                        ParticipantName = colEntity is not null ? $"{colEntity.Nombre} {colEntity.Apellido}" : string.Empty,
                                        TrainingName = capacitacion.NombreCertificado ?? capacitacion.PlantillaNombreCertificado ?? capacitacion.Nombre,
                                        IssuedDate = DateTime.Today,
                                        DurationHours = capacitacion.DuracionHoras,
                                        CertificateCode = $"C-{inscripcion.ColaboradorId}-{capacitacion.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                                    };

                                    pdfBytes = certificatePdfGenerator.Generate(pdfData);
                                }
                            }
                            catch
                            {
                                /* No bloquear si falla la generacion del PDF */
                            }

                            var certificado = new Certificado
                            {
                                Nombre = nombreCert,
                                Institucion = "NovaHub",
                                FechaEmision = DateTime.Today,
                                ColaboradorId = inscripcion.ColaboradorId,
                                CapacitacionId = capacitacion.Id
                            };

                            if (pdfBytes is not null && pdfBytes.Length > 0)
                            {
                                await certificadoService.CreateAsync(certificado, pdfBytes, colEntity?.Email);
                            }
                            else
                            {
                                await certificadoRepository.CreateAsync(certificado);
                            }

                            certificadoEmitido = true;
                            nombreCertificado = nombreCert;
                        }
                    }
                }
            }
            catch { /* No bloquear el resultado si falla la emisión del certificado */ }
        }

        await FinalizarCapacitacionSiCorrespondeAsync(cuestionario.CapacitacionId);
        Console.WriteLine($"[DEBUG CuestionarioService.ResponderAsync] Finalización verificada para CapacitacionId={cuestionario.CapacitacionId}.");

        var finalizadoRes = aprobado || nuevosIntentosRealizados >= cuestionario.IntentosPermitidos;
        var fechaFinalizacionRes = finalizadoRes ? (DateTime?)respuesta.FechaRespuesta : null;

        return new ResultadoCuestionarioDto
        {
            Puntaje = puntaje,
            Aprobado = aprobado,
            PuntajeAprobacion = cuestionario.PuntajeAprobacion,
            AprobacionPorCorrectas = cuestionario.AprobacionPorCorrectas,
            MinCorrectas = cuestionario.MinCorrectas,
            TotalPreguntas = total,
            Correctas = correctas,
            CertificadoEmitido = certificadoEmitido,
            NombreCertificado = nombreCertificado,
            IntentosMaximos = cuestionario.IntentosPermitidos,
            IntentosRealizados = nuevosIntentosRealizados,
            PuedeResponderOtroIntento = !aprobado && nuevosIntentosRealizados < cuestionario.IntentosPermitidos,
            FechaFinalizacion = fechaFinalizacionRes
        };
    }

    public async Task<ResultadoCuestionarioDto?> GetResultadoAsync(int cuestionarioId, int inscripcionId)
    {
        Console.WriteLine($"[DEBUG CuestionarioService.GetResultadoAsync] Buscando resultado. CuestionarioId={cuestionarioId}, InscripcionId={inscripcionId}");
        var respuestas = await repository.GetRespuestasAsync(cuestionarioId, inscripcionId);
        var c = await repository.GetByIdAsync(cuestionarioId);
        if (c is null) return null;

        var intentosMaximos = c.IntentosPermitidos;
        var intentosRealizados = respuestas.Count;

        if (!respuestas.Any())
        {
            Console.WriteLine($"[DEBUG CuestionarioService.GetResultadoAsync] No existe respuesta previa para CuestionarioId={cuestionarioId}, InscripcionId={inscripcionId}");
            return new ResultadoCuestionarioDto
            {
                Puntaje = 0,
                Aprobado = false,
                PuntajeAprobacion = c.PuntajeAprobacion,
                AprobacionPorCorrectas = c.AprobacionPorCorrectas,
                MinCorrectas = c.MinCorrectas,
                TotalPreguntas = c.Preguntas.Count,
                Correctas = 0,
                IntentosMaximos = intentosMaximos,
                IntentosRealizados = 0,
                PuedeResponderOtroIntento = true,
                FechaFinalizacion = null
            };
        }

        var mejorRespuesta = respuestas.FirstOrDefault(r => r.Aprobado) 
            ?? respuestas.OrderByDescending(r => r.FechaRespuesta).First();

        var aprobado = respuestas.Any(r => r.Aprobado);
        var finalizado = aprobado || intentosRealizados >= intentosMaximos;
        var fechaFinalizacion = finalizado ? (DateTime?)mejorRespuesta.FechaRespuesta : null;

        Console.WriteLine($"[DEBUG CuestionarioService.GetResultadoAsync] Respuestas encontradas: {intentosRealizados}. Aprobado={aprobado}");
        return new ResultadoCuestionarioDto
        {
            Puntaje = mejorRespuesta.Puntaje,
            Aprobado = mejorRespuesta.Aprobado,
            PuntajeAprobacion = c.PuntajeAprobacion,
            AprobacionPorCorrectas = c.AprobacionPorCorrectas,
            MinCorrectas = c.MinCorrectas,
            TotalPreguntas = c.Preguntas.Count,
            Correctas = mejorRespuesta.TotalCorrectas,
            IntentosMaximos = intentosMaximos,
            IntentosRealizados = intentosRealizados,
            PuedeResponderOtroIntento = !aprobado && intentosRealizados < intentosMaximos,
            FechaFinalizacion = fechaFinalizacion
        };
    }

    /// <summary>
    /// Devuelve los IDs de capacitaciones completadas por el colaborador en una sola query.
    /// Reemplaza el N+1 de CargarAprobadas en Capacitaciones.razor.
    /// </summary>
    public Task<List<int>> GetCapacitacionesAprobadasAsync(int colaboradorId) =>
        repository.GetCapacitacionesAprobadasPorColaboradorAsync(colaboradorId);

    private async Task FinalizarCapacitacionSiCorrespondeAsync(int capacitacionId)
    {
        // Obtener capacitación
        var capacitacion = await capacitacionRepository.GetByIdAsync(capacitacionId);
        if (capacitacion is null)
        {
            Console.WriteLine($"[DEBUG CuestionarioService.FinalizarCapacitacionSiCorrespondeAsync] CapacitacionId={capacitacionId} no encontrada.");
            return;
        }

        if (capacitacion.Finalizada)
        {
            Console.WriteLine($"[DEBUG CuestionarioService.FinalizarCapacitacionSiCorrespondeAsync] CapacitacionId={capacitacionId} ya estaba finalizada.");
            return;
        }

        // Obtener inscripciones activas
        var inscripcionesActivas = await inscripcionRepository.GetByCapacitacionAsync(capacitacionId);
        var totalInscritos = inscripcionesActivas.Count();

        // Si no hay inscritos, no finalizar
        if (totalInscritos == 0)
        {
            Console.WriteLine($"[DEBUG CuestionarioService.FinalizarCapacitacionSiCorrespondeAsync] CapacitacionId={capacitacionId} no tiene inscripciones activas.");
            return;
        }

        // Contar cuántos inscritos han respondido el cuestionario
        var totalRespondieron = await repository.ContarRespuestasCapacitacionAsync(capacitacionId);
        Console.WriteLine($"[DEBUG CuestionarioService.FinalizarCapacitacionSiCorrespondeAsync] CapacitacionId={capacitacionId}: {totalRespondieron}/{totalInscritos} respondieron.");

        // Si todos los inscritos han respondido (aunque no todos aprueben), marcar como finalizada
        if (totalRespondieron >= totalInscritos)
        {
            capacitacion.Finalizada = true;
            capacitacion.FechaFinalizacion = DateTime.UtcNow;
            capacitacion.MotivoFinalizacion = "Finalizada automáticamente: todos los colaboradores inscritos completaron su evaluación.";
            await capacitacionRepository.UpdateAsync(capacitacion);

            await auditLogRepository.CreateAsync(new AuditLog
            {
                EntidadTipo = nameof(Capacitacion),
                EntidadId = capacitacion.Id,
                EntidadNombre = capacitacion.Nombre,
                Accion = "Finalizada",
                FechaHora = DateTime.UtcNow,
                Observaciones = $"Finalización automática: {totalRespondieron}/{totalInscritos} colaboradores completaron la evaluación."
            });

            Console.WriteLine($"[DEBUG CuestionarioService.FinalizarCapacitacionSiCorrespondeAsync] CapacitacionId={capacitacionId} marcada como finalizada.");
        }
        else
        {
            Console.WriteLine($"[DEBUG CuestionarioService.FinalizarCapacitacionSiCorrespondeAsync] CapacitacionId={capacitacionId} no finalizada porque faltan respuestas.");
        }
    }

    private static CuestionarioDto MapToDto(Cuestionario c) => new()
    {
        Id = c.Id,
        Titulo = c.Titulo,
        Descripcion = c.Descripcion,
        PuntajeAprobacion = c.PuntajeAprobacion,
        AprobacionPorCorrectas = c.AprobacionPorCorrectas,
        MinCorrectas = c.MinCorrectas,
        IntentosPermitidos = c.IntentosPermitidos,
        CapacitacionId = c.CapacitacionId,
        Preguntas = c.Preguntas.OrderBy(p => p.Orden).Select(p => new PreguntaDto
        {
            Id = p.Id,
            Enunciado = p.Enunciado,
            Orden = p.Orden,
            Opciones = p.Opciones.OrderBy(o => o.Orden).Select(o => new OpcionDto
            {
                Id = o.Id,
                Texto = o.Texto,
                EsCorrecta = o.EsCorrecta,
                Orden = o.Orden
            }).ToList()
        }).ToList()
    };
}
