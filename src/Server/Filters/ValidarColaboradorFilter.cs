using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
using TalentManagement.Server.Services;

namespace TalentManagement.Server.Filters;

public class ValidarColaboradorFilter(CurrentUserService currentUser) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var path = httpContext.Request.Path.Value?.ToLower() ?? "";

            // Excluir endpoints de autenticación y salud
            if (!path.Contains("/api/v1/auth/perfil") &&
                !path.Contains("/api/v1/auth/es-microsoft") &&
                !path.Contains("/health"))
            {
                var colaboradorId = await currentUser.GetColaboradorIdAsync();
                if (colaboradorId is null)
                {
                    context.Result = new ObjectResult(new { message = "El usuario no está registrado como colaborador." })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
        }

        await next();
    }
}
