using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Notifications;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Infrastructure.Services;

public class CertificationExpiryService(SolodocDbContext db, IEmailService emailService, ILogger<CertificationExpiryService> logger) : ICertificationExpiryService
{
    public async Task<int> CheckExpiriesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var in30Days = today.AddDays(30);
        var in90Days = today.AddDays(90);

        var expiringCerts = await db.EmployeeCertifications
            .Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value <= in90Days)
            .Select(c => new
            {
                c.PersonId,
                c.TenantId,
                c.Name,
                c.ExpiryDate
            })
            .ToListAsync(ct);

        var notificationsCreated = 0;

        foreach (var cert in expiringCerts)
        {
            if (!cert.ExpiryDate.HasValue) continue;

            var daysUntilExpiry = cert.ExpiryDate.Value.DayNumber - today.DayNumber;
            var urgency = daysUntilExpiry switch
            {
                <= 0 => "Utløpt",
                <= 30 => "Utløper om mindre enn 30 dager",
                _ => "Utløper om mindre enn 90 dager"
            };

            // Check if notification already exists for this cert today
            var alreadyNotified = await db.Notifications
                .AnyAsync(n =>
                    n.PersonId == cert.PersonId &&
                    n.Type == "CertificationExpiry" &&
                    n.Title.Contains(cert.Name) &&
                    n.CreatedAt.Date == DateTimeOffset.UtcNow.Date, ct);

            if (alreadyNotified) continue;

            var notification = new Notification
            {
                PersonId = cert.PersonId,
                TenantId = cert.TenantId,
                Title = $"Sertifikat: {cert.Name} - {urgency}",
                Message = $"Sertifikatet \"{cert.Name}\" utløper {cert.ExpiryDate.Value:dd.MM.yyyy}.",
                Type = "CertificationExpiry",
                LinkUrl = "/employees/certifications"
            };

            db.Notifications.Add(notification);
            notificationsCreated++;

            // Send email notification
            try
            {
                var personEmail = await db.Persons
                    .Where(p => p.Id == cert.PersonId)
                    .Select(p => new { p.Email, p.FullName })
                    .FirstOrDefaultAsync(ct);

                if (personEmail is not null && !string.IsNullOrWhiteSpace(personEmail.Email))
                {
                    await emailService.SendCertificationExpiryWarningAsync(
                        personEmail.Email, personEmail.FullName, cert.Name, cert.ExpiryDate.Value, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send certification expiry email for {CertName} to person {PersonId}",
                    cert.Name, cert.PersonId);
            }
        }

        if (notificationsCreated > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation("Certification expiry check: {Count} notifications created", notificationsCreated);
        return notificationsCreated;
    }
}

public class DeviationReminderService(SolodocDbContext db, ILogger<DeviationReminderService> logger) : IDeviationReminderService
{
    public async Task<int> SendOverdueRemindersAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var overdueDeviations = await db.Deviations
            .Where(d =>
                d.Status != DeviationStatus.Closed &&
                d.CorrectiveActionDeadline.HasValue &&
                d.CorrectiveActionDeadline.Value < now &&
                !d.CorrectiveActionCompletedAt.HasValue)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.TenantId,
                d.AssignedToId,
                d.ReportedById,
                d.CorrectiveActionDeadline
            })
            .ToListAsync(ct);

        var notificationsCreated = 0;

        foreach (var deviation in overdueDeviations)
        {
            var recipientId = deviation.AssignedToId ?? deviation.ReportedById;

            var alreadyNotified = await db.Notifications
                .AnyAsync(n =>
                    n.PersonId == recipientId &&
                    n.Type == "OverdueDeviation" &&
                    n.LinkUrl == $"/deviations/{deviation.Id}" &&
                    n.CreatedAt.Date == now.Date, ct);

            if (alreadyNotified) continue;

            var notification = new Notification
            {
                PersonId = recipientId,
                TenantId = deviation.TenantId,
                Title = $"Forfalt avvik: {deviation.Title}",
                Message = $"Korrigerende tiltak for avviket \"{deviation.Title}\" var forfalt {deviation.CorrectiveActionDeadline!.Value:dd.MM.yyyy}.",
                Type = "OverdueDeviation",
                LinkUrl = $"/deviations/{deviation.Id}"
            };

            db.Notifications.Add(notification);
            notificationsCreated++;
        }

        if (notificationsCreated > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation("Overdue deviation reminders: {Count} notifications created", notificationsCreated);
        return notificationsCreated;
    }

    public async Task<int> AutoEscalateAsync(CancellationToken ct)
    {
        var twentyFourHoursAgo = DateTimeOffset.UtcNow.AddHours(-24);

        var deviationsToEscalate = await db.Deviations
            .Where(d =>
                d.Status == DeviationStatus.Open &&
                d.Severity == DeviationSeverity.High &&
                d.CreatedAt < twentyFourHoursAgo)
            .ToListAsync(ct);

        var escalatedCount = 0;

        foreach (var deviation in deviationsToEscalate)
        {
            deviation.Status = DeviationStatus.InProgress;

            var recipientId = deviation.AssignedToId ?? deviation.ReportedById;

            var notification = new Notification
            {
                PersonId = recipientId,
                TenantId = deviation.TenantId,
                Title = $"Avvik eskalert: {deviation.Title}",
                Message = $"Avviket \"{deviation.Title}\" med alvorlighetsgrad {deviation.Severity} har blitt automatisk eskalert til \"Under behandling\".",
                Type = "DeviationEscalation",
                LinkUrl = $"/deviations/{deviation.Id}"
            };

            db.Notifications.Add(notification);
            escalatedCount++;
        }

        if (escalatedCount > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation("Deviation auto-escalation: {Count} deviations escalated", escalatedCount);
        return escalatedCount;
    }
}

public class ScheduledActivityService(SolodocDbContext db, ILogger<ScheduledActivityService> logger) : IScheduledActivityService
{
    public async Task<int> CheckMissedActivitiesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var notificationsCreated = 0;

        // Check overdue safety rounds
        var overdueRounds = await db.SafetyRoundSchedules
            .Where(s => s.IsActive && s.NextDue < today)
            .ToListAsync(ct);

        foreach (var round in overdueRounds)
        {
            // Create notification for tenant admins — we use TenantId as a placeholder recipient
            // In a full implementation, we'd look up tenant admin PersonIds
            var notification = new Notification
            {
                PersonId = Guid.Empty, // Will be resolved by notification delivery system
                TenantId = round.TenantId,
                Title = $"Forfalt vernerunde: {round.Name}",
                Message = $"Vernerunden \"{round.Name}\" var planlagt {round.NextDue:dd.MM.yyyy} og er ikke gjennomført.",
                Type = "MissedSafetyRound",
                LinkUrl = "/hms/safety-rounds"
            };

            db.Notifications.Add(notification);
            notificationsCreated++;
        }

        // Check HMS meetings scheduled in the past with no minutes
        var missedMeetings = await db.HmsMeetings
            .Where(m => m.Date < today)
            .Where(m => !db.HmsMeetingMinutes.Any(min => min.MeetingId == m.Id))
            .Select(m => new { m.Id, m.Title, m.Date, m.TenantId, m.CreatedById })
            .ToListAsync(ct);

        foreach (var meeting in missedMeetings)
        {
            var alreadyNotified = await db.Notifications
                .AnyAsync(n =>
                    n.Type == "MissedHmsMeeting" &&
                    n.LinkUrl == $"/hms/meetings/{meeting.Id}", ct);

            if (alreadyNotified) continue;

            var notification = new Notification
            {
                PersonId = meeting.CreatedById,
                TenantId = meeting.TenantId,
                Title = $"HMS-møte uten referat: {meeting.Title}",
                Message = $"HMS-møtet \"{meeting.Title}\" ({meeting.Date:dd.MM.yyyy}) har ikke fått registrert referat.",
                Type = "MissedHmsMeeting",
                LinkUrl = $"/hms/meetings/{meeting.Id}"
            };

            db.Notifications.Add(notification);
            notificationsCreated++;
        }

        if (notificationsCreated > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation("Scheduled activity check: {Count} notifications created", notificationsCreated);
        return notificationsCreated;
    }
}

public class DataMaintenanceService(SolodocDbContext db, ILogger<DataMaintenanceService> logger) : IDataMaintenanceService
{
    public async Task<int> CleanupSyncQueueAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        var oldSyncEvents = await db.AuditEvents
            .Where(e => e.Action == "Sync" && e.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (oldSyncEvents.Count == 0) return 0;

        db.AuditEvents.RemoveRange(oldSyncEvents);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Sync queue cleanup: {Count} old sync events removed", oldSyncEvents.Count);
        return oldSyncEvents.Count;
    }

    public async Task<int> AnonymizeExpiredDataAsync(CancellationToken ct)
    {
        var fiveYearsAgo = DateTimeOffset.UtcNow.AddYears(-5);

        // Find persons whose ALL tenant memberships have been removed more than 5 years ago
        var personsToAnonymize = await db.Persons
            .IgnoreQueryFilters()
            .Where(p => p.FullName != "Anonymisert bruker")
            .Where(p => db.TenantMemberships
                .IgnoreQueryFilters()
                .Where(m => m.PersonId == p.Id)
                .All(m => m.State == TenantMembershipState.Removed && m.RemovedAt.HasValue && m.RemovedAt.Value < fiveYearsAgo))
            .Where(p => db.TenantMemberships
                .IgnoreQueryFilters()
                .Any(m => m.PersonId == p.Id)) // Must have had at least one membership
            .ToListAsync(ct);

        foreach (var person in personsToAnonymize)
        {
            person.FullName = "Anonymisert bruker";
            person.Email = $"anonymized-{person.Id}@solodoc.dev";
            person.PhoneNumber = null;
            person.PasswordHash = string.Empty;
        }

        if (personsToAnonymize.Count > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation("Data anonymization: {Count} persons anonymized", personsToAnonymize.Count);
        return personsToAnonymize.Count;
    }

    public async Task<int> ArchiveOldAuditDataAsync(CancellationToken ct)
    {
        var twelveMonthsAgo = DateTimeOffset.UtcNow.AddMonths(-12);

        // Mark old audit events by adding an "Archived" detail
        var oldEvents = await db.AuditEvents
            .Where(e => e.CreatedAt < twelveMonthsAgo && (e.Details == null || !e.Details.Contains("[Archived]")))
            .ToListAsync(ct);

        foreach (var evt in oldEvents)
        {
            evt.Details = string.IsNullOrEmpty(evt.Details)
                ? "[Archived]"
                : $"{evt.Details} [Archived]";
        }

        if (oldEvents.Count > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation("Audit archival: {Count} events marked as archived", oldEvents.Count);
        return oldEvents.Count;
    }
}

public class SystemHealthService(SolodocDbContext db, ILogger<SystemHealthService> logger) : ISystemHealthService
{
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct)
    {
        var databaseOk = false;

        try
        {
            databaseOk = await db.Database.CanConnectAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database health check failed");
        }

        // Storage check is a placeholder — will be implemented with IFileStorageService
        var storageOk = true;

        var result = new HealthCheckResult(databaseOk, storageOk, DateTimeOffset.UtcNow);

        if (!databaseOk)
            logger.LogWarning("System health check: Database is NOT healthy");

        return result;
    }
}
