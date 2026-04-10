using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solodoc.Application.Services;

namespace Solodoc.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpClient _smtp;
    private readonly string _fromAddress;
    private readonly string _clientBaseUrl;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        var host = configuration["Email:SmtpHost"] ?? "localhost";
        var port = int.Parse(configuration["Email:SmtpPort"] ?? "1025");
        _fromAddress = configuration["Email:FromAddress"] ?? "noreply@solodoc.dev";
        _clientBaseUrl = configuration["Email:ClientBaseUrl"] ?? "http://localhost:5063";

        _smtp = new SmtpClient(host, port)
        {
            EnableSsl = false,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MailMessage(_fromAddress, to, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        try
        {
            await _smtp.SendMailAsync(message, ct);
            _logger.LogInformation("Email sent to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            throw;
        }
    }

    public async Task SendInvitationAsync(string toEmail, string inviterName, string tenantName, string role, Guid invitationId, CancellationToken ct = default)
    {
        var acceptUrl = $"{_clientBaseUrl}/invite/{invitationId}";
        var subject = $"Du er invitert til {tenantName} på Solodoc";
        var body = WrapInLayout($"""
            <h2 style="margin:0 0 16px 0; color:#1a1a1a; font-size:22px;">Invitasjon til {HtmlEncode(tenantName)}</h2>
            <p style="margin:0 0 12px 0; color:#333; font-size:15px; line-height:1.5;">
                <strong>{HtmlEncode(inviterName)}</strong> har invitert deg som <strong>{HtmlEncode(role)}</strong> i <strong>{HtmlEncode(tenantName)}</strong>.
            </p>
            <p style="margin:0 0 24px 0; color:#333; font-size:15px; line-height:1.5;">
                Klikk knappen under for å akseptere invitasjonen.
            </p>
            <div style="text-align:center; margin:24px 0;">
                <a href="{acceptUrl}" style="display:inline-block; background-color:#2563eb; color:#ffffff; text-decoration:none; padding:12px 32px; border-radius:6px; font-size:15px; font-weight:600;">
                    Aksepter invitasjon
                </a>
            </div>
            <p style="margin:24px 0 0 0; color:#666; font-size:13px; line-height:1.4;">
                Hvis knappen ikke fungerer, kopier denne lenken til nettleseren din:<br/>
                <a href="{acceptUrl}" style="color:#2563eb; word-break:break-all;">{acceptUrl}</a>
            </p>
            """);

        await SendAsync(toEmail, subject, body, ct);
    }

    public async Task SendDeviationNotificationAsync(string toEmail, string deviationTitle, string projectName, string action, CancellationToken ct = default)
    {
        var subject = $"Avvik: {deviationTitle}";
        var body = WrapInLayout($"""
            <h2 style="margin:0 0 16px 0; color:#1a1a1a; font-size:22px;">Avviksoppdatering</h2>
            <p style="margin:0 0 12px 0; color:#333; font-size:15px; line-height:1.5;">
                <strong>{HtmlEncode(action)}</strong> på prosjekt <strong>{HtmlEncode(projectName)}</strong>.
            </p>
            <table style="margin:16px 0; border-collapse:collapse; width:100%;">
                <tr>
                    <td style="padding:8px 12px; background:#f3f4f6; border:1px solid #e5e7eb; font-weight:600; color:#374151; width:120px;">Avvik</td>
                    <td style="padding:8px 12px; border:1px solid #e5e7eb; color:#333;">{HtmlEncode(deviationTitle)}</td>
                </tr>
                <tr>
                    <td style="padding:8px 12px; background:#f3f4f6; border:1px solid #e5e7eb; font-weight:600; color:#374151;">Prosjekt</td>
                    <td style="padding:8px 12px; border:1px solid #e5e7eb; color:#333;">{HtmlEncode(projectName)}</td>
                </tr>
                <tr>
                    <td style="padding:8px 12px; background:#f3f4f6; border:1px solid #e5e7eb; font-weight:600; color:#374151;">Handling</td>
                    <td style="padding:8px 12px; border:1px solid #e5e7eb; color:#333;">{HtmlEncode(action)}</td>
                </tr>
            </table>
            """);

        await SendAsync(toEmail, subject, body, ct);
    }

    public async Task SendCertificationExpiryWarningAsync(string toEmail, string employeeName, string certName, DateOnly expiryDate, CancellationToken ct = default)
    {
        var daysUntilExpiry = expiryDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        var urgencyColor = daysUntilExpiry <= 0 ? "#dc2626" : daysUntilExpiry <= 30 ? "#d97706" : "#2563eb";
        var urgencyText = daysUntilExpiry <= 0 ? "Utl\u00f8pt" : $"Utl\u00f8per om {daysUntilExpiry} dager";

        var subject = $"Sertifikat utl\u00f8per snart: {certName}";
        var body = WrapInLayout($"""
            <h2 style="margin:0 0 16px 0; color:#1a1a1a; font-size:22px;">Sertifikatvarsel</h2>
            <p style="margin:0 0 12px 0; color:#333; font-size:15px; line-height:1.5;">
                <strong>{HtmlEncode(employeeName)}</strong>, sertifikatet <strong>{HtmlEncode(certName)}</strong> utl&oslash;per <strong>{expiryDate:dd.MM.yyyy}</strong>.
            </p>
            <div style="margin:16px 0; padding:12px 16px; border-left:4px solid {urgencyColor}; background:#f9fafb; border-radius:0 6px 6px 0;">
                <span style="color:{urgencyColor}; font-weight:600; font-size:14px;">{urgencyText}</span>
            </div>
            <p style="margin:16px 0 0 0; color:#333; font-size:15px; line-height:1.5;">
                Vennligst sørg for å fornye sertifikatet i tide.
            </p>
            """);

        await SendAsync(toEmail, subject, body, ct);
    }

    private static string WrapInLayout(string content)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="nb">
            <head><meta charset="utf-8"/></head>
            <body style="margin:0; padding:0; background-color:#f3f4f6; font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;">
                <div style="max-width:560px; margin:32px auto; background:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 1px 3px rgba(0,0,0,0.1);">
                    <div style="background:#1e293b; padding:20px 24px;">
                        <span style="color:#ffffff; font-size:20px; font-weight:700; letter-spacing:-0.5px;">Solodoc</span>
                    </div>
                    <div style="padding:24px;">
                        {content}
                    </div>
                    <div style="padding:16px 24px; background:#f9fafb; border-top:1px solid #e5e7eb;">
                        <p style="margin:0; color:#9ca3af; font-size:12px; text-align:center;">
                            Denne e-posten er sendt fra Solodoc. Ikke svar p&aring; denne e-posten.
                        </p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }

    private static string HtmlEncode(string text) =>
        System.Net.WebUtility.HtmlEncode(text);
}
