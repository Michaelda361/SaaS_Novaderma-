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
    builder.Configuration.Bind("MsalProviderOptions", options.ProviderOptions);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(
        "api://60ea78c7-4add-4d9e-ba23-ac5d2c1ee4ac/access_as_user");
    options.ProviderOptions.AdditionalScopesToConsent.Add(
        "https://graph.microsoft.com/Files.Read");
    options.ProviderOptions.AdditionalScopesToConsent.Add(
        "https://graph.microsoft.com/Files.ReadWrite");
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Authentication.PostLogoutRedirectUri = null;
});

// Configuración global de JSON
builder.Services.AddSingleton(new System.Text.Json.JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

// HttpClient con Bearer token
builder.Services.AddScoped<ApiAuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ApiAuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5194/") };
    http.DefaultRequestHeaders.Add("Accept", "application/json");
    return http;
});

builder.Services.AddScoped<ColaboradorApiService>();
builder.Services.AddScoped<CapacitacionApiService>();
builder.Services.AddScoped<CertificadoApiService>();
builder.Services.AddScoped<AreaApiService>();
builder.Services.AddScoped<CargoApiService>();
builder.Services.AddScoped<InscripcionApiService>();
builder.Services.AddScoped<RecursoApiService>();
builder.Services.AddScoped<DocumentoApiService>();
builder.Services.AddScoped<PlantillaDocumentoApiService>();
builder.Services.AddScoped<OneDrivePickerService>();
builder.Services.AddScoped<OneDriveGraphService>();
builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
