using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TalentManagement.Client;
using TalentManagement.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// MSAL
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(
        "api://60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac/access_as_user");
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Authentication.PostLogoutRedirectUri = null;
});

// HttpClient con Bearer token — patrón correcto para Blazor WASM sin Microsoft.Extensions.Http
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(
            authorizedUrls: ["http://localhost:5194/"],
            scopes: ["api://60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac/access_as_user"]);

    // Asignar el inner handler del navegador explícitamente
    handler.InnerHandler = new HttpClientHandler();

    return new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:5194/")
    };
});

builder.Services.AddScoped<ColaboradorApiService>();
builder.Services.AddScoped<CapacitacionApiService>();
builder.Services.AddScoped<CertificadoApiService>();
builder.Services.AddScoped<AreaApiService>();
builder.Services.AddScoped<CargoApiService>();
builder.Services.AddScoped<InscripcionApiService>();
builder.Services.AddScoped<RecursoApiService>();

await builder.Build().RunAsync();
