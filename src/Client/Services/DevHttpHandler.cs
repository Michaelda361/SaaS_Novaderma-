namespace TalentManagement.Client.Services;

/// <summary>
/// En Development, si hay un usuario dev seleccionado, añade X-Dev-User al request
/// y omite el token MSAL. En producción nunca se registra.
/// </summary>
public class DevHttpHandler(DevAuthService devAuth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var email = await devAuth.GetEmailAsync();
        if (!string.IsNullOrWhiteSpace(email))
            request.Headers.TryAddWithoutValidation("X-Dev-User", email);

        return await base.SendAsync(request, cancellationToken);
    }
}
