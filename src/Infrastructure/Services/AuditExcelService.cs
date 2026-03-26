using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;

namespace TalentManagement.Infrastructure.Services;

public class AuditExcelService : IAuditExcelService
{
    private readonly TokenCredential _credential;
    private readonly string _siteId;
    private readonly string _excelPath;
    private const string SheetName = "Auditoria";
    private static readonly string[] Headers = ["ID","Fecha","Hora","Entidad","ID Entidad","Nombre Entidad","Accion","Colaborador","Observaciones","Cambios"];

    public AuditExcelService(IConfiguration config)
    {
        var sp = config.GetSection("SharePoint");
        _credential = new ClientSecretCredential(sp["TenantId"], sp["ClientId"], sp["ClientSecret"]);
        _siteId = sp["SiteId"] ?? throw new InvalidOperationException("SharePoint:SiteId no configurado");
        _excelPath = sp["AuditoriaExcelPath"] ?? "Auditoria/auditoria_documentos.xlsx";
    }

    public async Task AppendRowAsync(AuditLog log)
    {
        try
        {
            using var http = await BuildHttpClientAsync();
            var db = $"https://graph.microsoft.com/v1.0/sites/{_siteId}/drive";
            var itemId = await EnsureExcelAsync(http, db);
            var used = await GetJsonAsync(http, $"{db}/items/{itemId}/workbook/worksheets/{SheetName}/usedRange");
            var next = (used.TryGetProperty("rowCount", out var rc) ? rc.GetInt32() : 1) + 1;
            var ec = (char)('A' + Headers.Length - 1);
            var vals = new[]{ new[]{ log.Id.ToString(), log.FechaHora.ToLocalTime().ToString("dd/MM/yyyy"), log.FechaHora.ToLocalTime().ToString("HH:mm:ss"), log.EntidadTipo, log.EntidadId.ToString(), log.EntidadNombre, log.Accion, log.ColaboradorNombre, log.Observaciones??"", log.CamposModificados??"" }};
            await PatchRangeAsync(http, db, itemId, $"A{next}:{ec}{next}", vals);
        }
        catch (Exception ex) { Console.Error.WriteLine($"[AuditExcel] {ex.Message}"); }
    }

    private async Task<HttpClient> BuildHttpClientAsync()
    {
        var t = await _credential.GetTokenAsync(new TokenRequestContext(["https://graph.microsoft.com/.default"]), CancellationToken.None);
        var h = new HttpClient();
        h.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t.Token);
        return h;
    }

    private async Task<string> EnsureExcelAsync(HttpClient http, string db)
    {
        var r = await http.GetAsync($"{db}/root:/{_excelPath}");
        if (r.IsSuccessStatusCode) { var j = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return j.RootElement.GetProperty("id").GetString()!; }
        return await CreateExcelAsync(http, db);
    }

    private async Task<string> CreateExcelAsync(HttpClient http, string db)
    {
        var e = new ByteArrayContent(Array.Empty<byte>()); e.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var up = await http.PutAsync($"{db}/root:/{_excelPath}:/content", e); up.EnsureSuccessStatusCode();
        var j = JsonDocument.Parse(await up.Content.ReadAsStringAsync());
        var id = j.RootElement.GetProperty("id").GetString()!;
        try { var sh = await GetJsonAsync(http, $"{db}/items/{id}/workbook/worksheets"); if (sh.TryGetProperty("value", out var arr) && arr.GetArrayLength()>0) { var sid=arr[0].GetProperty("id").GetString()!; if(arr[0].GetProperty("name").GetString()!=SheetName) { await http.PatchAsync($"{db}/items/{id}/workbook/worksheets/{sid}", new StringContent(System.Text.Json.JsonSerializer.Serialize(new{name=SheetName}),Encoding.UTF8,"application/json")); } } } catch{}
        var ec=(char)('A'+Headers.Length-1); await PatchRangeAsync(http,db,id,$"A1:{ec}1",new[]{Headers}); return id;
    }

    private async Task<JsonElement> GetJsonAsync(HttpClient http, string url)
    { var r=await http.GetAsync(url); r.EnsureSuccessStatusCode(); return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone(); }

    private async Task PatchRangeAsync(HttpClient http, string db, string id, string addr, string[][] vals)
    {
        var body=new StringContent(System.Text.Json.JsonSerializer.Serialize(new{values=vals}),Encoding.UTF8,"application/json");
        var url=$"{db}/items/{id}/workbook/worksheets/{SheetName}/range(address='{addr}')";
        var req=new HttpRequestMessage(HttpMethod.Patch,url){Content=body};
        var resp=await http.SendAsync(req); resp.EnsureSuccessStatusCode();
    }
}
