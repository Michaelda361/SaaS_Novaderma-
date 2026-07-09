using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentManagement.Application.Interfaces;

namespace TalentManagement.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly string _emailsFolder;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
        _emailsFolder = GetEmailsDirectory();
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        _logger.LogInformation("Preparando envío de correo a: {To}, Asunto: {Subject}", to, subject);

        var server = _config["SmtpSettings:Server"];
        var portStr = _config["SmtpSettings:Port"];
        var username = _config["SmtpSettings:Username"];
        var password = _config["SmtpSettings:Password"];
        var senderEmail = _config["SmtpSettings:SenderEmail"] ?? "novahub-noreply@novaderma.local";
        var senderName = _config["SmtpSettings:SenderName"] ?? "NovaHub Capacitaciones";
        var enableSslStr = _config["SmtpSettings:EnableSsl"] ?? "true";
        var useMockStr = _config["SmtpSettings:UseMock"] ?? "false";

        bool useMock = bool.TryParse(useMockStr, out var mockVal) && mockVal;
        bool enableSsl = !bool.TryParse(enableSslStr, out var sslVal) || sslVal;
        int port = int.TryParse(portStr, out var pVal) ? pVal : 587;

        // Si no está configurado el servidor SMTP o el usuario, o si se fuerza el modo Mock, procedemos al modo simulado
        if (useMock || string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username) || server == "localhost")
        {
            await SaveEmailToLocalFileAsync(to, subject, body, senderEmail, senderName);
            return;
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            mailMessage.To.Add(to);

            using var smtpClient = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                Timeout = 15000 // 15 segundos de timeout
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Correo enviado exitosamente a {To} vía SMTP.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al enviar correo a {To} vía SMTP. Guardando en archivo local de respaldo.", to);
            // Fallback en caso de error SMTP: escribir a archivo local
            await SaveEmailToLocalFileAsync(to, subject, body, senderEmail, senderName);
        }
    }

    private async Task SaveEmailToLocalFileAsync(string to, string subject, string body, string fromEmail, string fromName)
    {
        try
        {
            var fileName = $"email_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.html";
            var filePath = Path.Combine(_emailsFolder, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine("<title>Simulación de Correo</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("  body { font-family: sans-serif; background-color: #f8fafc; padding: 20px; color: #1e293b; }");
            sb.AppendLine("  .headers { background: #e2e8f0; padding: 15px; border-radius: 8px 8px 0 0; font-size: 0.9em; border-bottom: 2px solid #cbd5e1; }");
            sb.AppendLine("  .body-content { background: #ffffff; padding: 20px; border-radius: 0 0 8px 8px; border: 1px solid #e2e8f0; border-top: none; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"headers\">");
            sb.AppendLine($"    <div><strong>De:</strong> {fromName} &lt;{fromEmail}&gt;</div>");
            sb.AppendLine($"    <div><strong>Para:</strong> {to}</div>");
            sb.AppendLine($"    <div><strong>Asunto:</strong> {subject}</div>");
            sb.AppendLine($"    <div><strong>Fecha (UTC):</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class=\"body-content\">");
            sb.AppendLine(body);
            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
            _logger.LogInformation("Simulación de correo guardada en: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo guardar la simulación de correo en un archivo local.");
        }
    }

    private string GetEmailsDirectory()
    {
        var contentRoot = _config["ContentRootPath"];
        if (!string.IsNullOrEmpty(contentRoot))
        {
            var path = Path.Combine(contentRoot, "scratch", "emails");
            Directory.CreateDirectory(path);
            return path;
        }

        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "TalentManagement.slnx")) || Directory.Exists(Path.Combine(dir, "src")))
            {
                var path = Path.Combine(dir, "scratch", "emails");
                Directory.CreateDirectory(path);
                return path;
            }
            dir = Path.GetDirectoryName(dir);
        }

        var fallbackPath = Path.Combine(AppContext.BaseDirectory, "scratch", "emails");
        Directory.CreateDirectory(fallbackPath);
        return fallbackPath;
    }
}
