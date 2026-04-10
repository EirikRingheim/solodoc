using Microsoft.EntityFrameworkCore;
using Quartz;
using Solodoc.Application.Services;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Infrastructure.Services;
using Solodoc.Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

// Database
builder.Services.AddDbContext<SolodocDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

// Services
builder.Services.AddScoped<ICertificationExpiryService, CertificationExpiryService>();
builder.Services.AddScoped<IDeviationReminderService, DeviationReminderService>();
builder.Services.AddScoped<IScheduledActivityService, ScheduledActivityService>();
builder.Services.AddScoped<IDataMaintenanceService, DataMaintenanceService>();
builder.Services.AddScoped<ISystemHealthService, SystemHealthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<IExportService, ExportService>();

// Quartz
builder.Services.AddQuartz(q =>
{
    // CertificationExpiryCheck — daily 06:00
    q.ScheduleJob<CertificationExpiryCheckJob>(trigger => trigger
        .WithIdentity("certification-expiry-trigger")
        .WithCronSchedule("0 0 6 * * ?")
        .WithDescription("Check for expiring certifications daily at 06:00"));

    // OverdueDeviationReminder — daily 08:00
    q.ScheduleJob<OverdueDeviationReminderJob>(trigger => trigger
        .WithIdentity("overdue-deviation-reminder-trigger")
        .WithCronSchedule("0 0 8 * * ?")
        .WithDescription("Send overdue deviation reminders daily at 08:00"));

    // DeviationAutoEscalation — hourly
    q.ScheduleJob<DeviationAutoEscalationJob>(trigger => trigger
        .WithIdentity("deviation-auto-escalation-trigger")
        .WithCronSchedule("0 0 * * * ?")
        .WithDescription("Auto-escalate high/critical open deviations every hour"));

    // ScheduledActivityCheck — daily 09:00
    q.ScheduleJob<ScheduledActivityCheckJob>(trigger => trigger
        .WithIdentity("scheduled-activity-check-trigger")
        .WithCronSchedule("0 0 9 * * ?")
        .WithDescription("Check for missed scheduled activities daily at 09:00"));

    // SyncQueueCleanup — hourly
    q.ScheduleJob<SyncQueueCleanupJob>(trigger => trigger
        .WithIdentity("sync-queue-cleanup-trigger")
        .WithCronSchedule("0 0 * * * ?")
        .WithDescription("Clean up old sync queue entries every hour"));

    // DataAnonymization — monthly, 1st at 02:00
    q.ScheduleJob<DataAnonymizationJob>(trigger => trigger
        .WithIdentity("data-anonymization-trigger")
        .WithCronSchedule("0 0 2 1 * ?")
        .WithDescription("Anonymize expired personal data monthly on the 1st at 02:00"));

    // AuditArchival — monthly, 1st at 03:00
    q.ScheduleJob<AuditArchivalJob>(trigger => trigger
        .WithIdentity("audit-archival-trigger")
        .WithCronSchedule("0 0 3 1 * ?")
        .WithDescription("Archive old audit data monthly on the 1st at 03:00"));

    // SystemHealthCheck — every 5 minutes
    q.ScheduleJob<SystemHealthCheckJob>(trigger => trigger
        .WithIdentity("system-health-check-trigger")
        .WithCronSchedule("0 0/5 * * * ?")
        .WithDescription("System health check every 5 minutes"));

    // SdsRevisionCheck — weekly Monday 07:00
    q.ScheduleJob<SdsRevisionCheckJob>(trigger => trigger
        .WithIdentity("sds-revision-check-trigger")
        .WithCronSchedule("0 0 7 ? * MON")
        .WithDescription("Check for SDS revisions weekly on Monday at 07:00"));

    // VehicleEuKontrollRefresh — monthly, 1st at 04:00
    q.ScheduleJob<VehicleEuKontrollRefreshJob>(trigger => trigger
        .WithIdentity("vehicle-eu-kontroll-refresh-trigger")
        .WithCronSchedule("0 0 4 1 * ?")
        .WithDescription("Refresh vehicle EU-kontroll data monthly on the 1st at 04:00"));

    // VacationYearEndRollover — January 1 at 00:30
    q.ScheduleJob<VacationYearEndRolloverJob>(trigger => trigger
        .WithIdentity("vacation-year-end-rollover-trigger")
        .WithCronSchedule("0 30 0 1 1 ?")
        .WithDescription("Vacation year-end rollover on January 1st at 00:30"));

    // ExportCleanup — daily 04:00
    q.ScheduleJob<ExportCleanupJob>(trigger => trigger
        .WithIdentity("export-cleanup-trigger")
        .WithCronSchedule("0 0 4 * * ?")
        .WithDescription("Clean up expired export files daily at 04:00"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var host = builder.Build();
host.Run();
