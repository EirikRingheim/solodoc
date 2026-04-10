namespace Solodoc.Application.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendInvitationAsync(string toEmail, string inviterName, string tenantName, string role, Guid invitationId, CancellationToken ct = default);
    Task SendDeviationNotificationAsync(string toEmail, string deviationTitle, string projectName, string action, CancellationToken ct = default);
    Task SendCertificationExpiryWarningAsync(string toEmail, string employeeName, string certName, DateOnly expiryDate, CancellationToken ct = default);
}
