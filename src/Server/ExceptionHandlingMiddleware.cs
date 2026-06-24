using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TalentManagement.Server;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió una excepción no controlada durante el procesamiento del request.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        string result;

        // Verificar si la excepción se originó en nuestro código
        bool esExcepcionPropia = exception.Source?.StartsWith("TalentManagement") == true ||
                                 exception.TargetSite?.DeclaringType?.Assembly.GetName().Name?.StartsWith("TalentManagement") == true ||
                                 (exception.StackTrace?.Contains("TalentManagement.") == true);

        switch (exception)
        {
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                result = esExcepcionPropia ? exception.Message : "No autorizado.";
                break;

            case ArgumentException:
            case InvalidOperationException:
                code = HttpStatusCode.BadRequest;
                result = esExcepcionPropia ? exception.Message : "Solicitud inválida.";
                break;

            case System.Collections.Generic.KeyNotFoundException:
                code = HttpStatusCode.NotFound;
                result = esExcepcionPropia ? exception.Message : "Recurso no encontrado.";
                break;

            default:
                code = HttpStatusCode.InternalServerError;
                result = "Ha ocurrido un error interno en el servidor.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var responsePayload = JsonSerializer.Serialize(new { error = result });
        return context.Response.WriteAsync(responsePayload);
    }
}
