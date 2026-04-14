using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Solodoc.Api.Middleware;
using Solodoc.Application.Auth;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Infrastructure.Services;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Auth;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;
using Solodoc.Shared.Common;
using Solodoc.Shared.Dashboard;
using Solodoc.Shared.Deviations;
using Solodoc.Api.Endpoints;
using System.Threading.RateLimiting;
using Solodoc.Shared.Projects;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<SolodocDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

// Tenant
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Auth services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<SeedDataService>();

// File storage (MinIO / S3)
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

// Translation (DeepL)
builder.Services.AddHttpClient("DeepL");
builder.Services.AddScoped<ITranslationService, TranslationService>();

// Audit trail
builder.Services.AddScoped<IAuditService, AuditService>();

// Email
builder.Services.AddSingleton<IEmailService, EmailService>();

// QuestPDF license (community)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// PDF reports
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

// Export
builder.Services.AddScoped<IExportService, ExportService>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// Rate limiting for auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: 10 attempts per minute per IP
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// CORS for Blazor client
var corsOrigins = builder.Configuration["Cors:Origins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5200", "https://localhost:5201", "http://localhost:5063", "https://localhost:7153", "http://192.168.1.226:5063"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// Auto-apply pending migrations on startup
using (var migrationScope = app.Services.CreateScope())
{
    var db = migrationScope.ServiceProvider.GetRequiredService<SolodocDbContext>();
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        Log.Information("Applying {Count} pending database migrations...", pendingMigrations.Count());
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<SeedDataService>();
    await seeder.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("BlazorClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LogEnrichmentMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

// Health
app.MapGet("/health", async (SolodocDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.StatusCode(503);
});

// Auth endpoints
app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    IValidator<RegisterRequest> validator,
    IAuthService authService,
    CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.ToDictionary());

    var result = await authService.RegisterAsync(request, ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    IValidator<LoginRequest> validator,
    IAuthService authService,
    CancellationToken ct) =>
{
    var validation = await validator.ValidateAsync(request, ct);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.ToDictionary());

    var result = await authService.LoginAsync(request, ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/refresh", async (
    RefreshTokenRequest request,
    IAuthService authService,
    CancellationToken ct) =>
{
    var result = await authService.RefreshAsync(request, ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Unauthorized();
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/logout", async (
    LogoutRequest request,
    IAuthService authService,
    CancellationToken ct) =>
{
    await authService.LogoutAsync(request.RefreshToken, ct);
    return Results.Ok();
});

app.MapGet("/api/auth/tenants", async (
    ClaimsPrincipal user,
    SolodocDbContext db,
    CancellationToken ct) =>
{
    var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(personIdClaim, out var personId))
        return Results.Unauthorized();

    var tenants = await db.TenantMemberships
        .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
        .Select(m => new Solodoc.Shared.Auth.TenantDto(
            m.TenantId,
            m.Tenant.Name,
            m.Tenant.OrgNumber,
            m.Role == TenantRole.TenantAdmin ? "Admin"
                : m.Role == TenantRole.ProjectLeader ? "Prosjektleder"
                : "Feltarbeider",
            m.Tenant.AccentColor))
        .ToListAsync(ct);

    return Results.Ok(tenants);
}).RequireAuthorization();

// Dashboard
app.MapGet("/api/dashboard/summary", async (
    ClaimsPrincipal user,
    SolodocDbContext db,
    CancellationToken ct) =>
{
    var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(personIdClaim, out var personId))
        return Results.Unauthorized();

    // Find the user's first tenant membership
    var membership = await db.TenantMemberships
        .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
        .FirstOrDefaultAsync(ct);

    if (membership is null)
        return Results.Ok(new DashboardSummaryDto(0, 0m, 0, [], []));

    var tenantId = membership.TenantId;

    var activeProjectCount = await db.Projects
        .CountAsync(p => p.TenantId == tenantId && p.Status == ProjectStatus.Active, ct);

    var firstOfMonth = new DateOnly(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1);
    var hoursThisMonth = await db.TimeEntries
        .Where(t => t.TenantId == tenantId && t.PersonId == personId && t.Date >= firstOfMonth)
        .SumAsync(t => t.Hours, ct);

    // Calculate hours this week (Monday to Sunday)
    var todayDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
    var dayOfWeek = todayDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)todayDate.DayOfWeek - 1;
    var startOfWeek = todayDate.AddDays(-dayOfWeek);
    var hoursThisWeek = await db.TimeEntries
        .Where(t => t.TenantId == tenantId && t.PersonId == personId && t.Date >= startOfWeek)
        .SumAsync(t => t.Hours, ct);

    var openDeviationCount = await db.Deviations
        .CountAsync(d => d.TenantId == tenantId && d.Status != DeviationStatus.Closed, ct);

    // Recent projects (up to 5, newest first)
    var recentProjects = await db.Projects
        .Where(p => p.TenantId == tenantId)
        .OrderByDescending(p => p.CreatedAt)
        .Take(5)
        .Select(p => new DashboardProjectDto(
            p.Id,
            p.Name,
            p.Status == ProjectStatus.Active ? "aktiv"
                : p.Status == ProjectStatus.Completed ? "fullført"
                : "arkivert"))
        .ToListAsync(ct);

    // Open deviations (up to 5, newest first)
    var openDeviations = await db.Deviations
        .Where(d => d.TenantId == tenantId && d.Status != DeviationStatus.Closed)
        .OrderByDescending(d => d.CreatedAt)
        .Take(5)
        .Select(d => new DashboardDeviationDto(
            d.Id,
            d.Title,
            d.Status == DeviationStatus.Open ? "Åpen" : "Under behandling",
            db.Projects
                .Where(p => p.Id == d.ProjectId)
                .Select(p => p.Name)
                .FirstOrDefault()))
        .ToListAsync(ct);

    return Results.Ok(new DashboardSummaryDto(
        activeProjectCount, hoursThisMonth, openDeviationCount,
        recentProjects, openDeviations, hoursThisWeek));
}).RequireAuthorization();

// Projects
app.MapGet("/api/projects", async (
    ClaimsPrincipal user,
    SolodocDbContext db,
    CancellationToken ct,
    int page = 1,
    int pageSize = 10,
    string? search = null,
    string? sortBy = null,
    bool sortDesc = false,
    bool topLevelOnly = false,
    Guid? parentId = null) =>
{
    var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(personIdClaim, out var personId))
        return Results.Unauthorized();

    var membership = await db.TenantMemberships
        .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
        .FirstOrDefaultAsync(ct);

    if (membership is null)
        return Results.Ok(new PagedResult<ProjectListItemDto>([], 0, page, pageSize));

    var tenantId = membership.TenantId;

    var query = db.Projects.Where(p => p.TenantId == tenantId);

    // Hierarchy filtering
    if (topLevelOnly)
        query = query.Where(p => p.ParentProjectId == null);
    if (parentId.HasValue)
        query = query.Where(p => p.ParentProjectId == parentId.Value);

    // Search
    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.ToLowerInvariant();
        query = query.Where(p =>
            p.Name.ToLower().Contains(term) ||
            (p.ClientName != null && p.ClientName.ToLower().Contains(term)) ||
            (p.Address != null && p.Address.ToLower().Contains(term)));
    }

    var totalCount = await query.CountAsync(ct);

    // Sort
    query = sortBy?.ToLowerInvariant() switch
    {
        "name" => sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
        "status" => sortDesc ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
        "clientname" => sortDesc ? query.OrderByDescending(p => p.ClientName) : query.OrderBy(p => p.ClientName),
        "startdate" => sortDesc ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
        "opendeviations" => sortDesc
            ? query.OrderByDescending(p => db.Deviations.Count(d => d.ProjectId == p.Id && d.Status != DeviationStatus.Closed))
            : query.OrderBy(p => db.Deviations.Count(d => d.ProjectId == p.Id && d.Status != DeviationStatus.Closed)),
        _ => query.OrderByDescending(p => p.CreatedAt)
    };

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProjectListItemDto(
            p.Id,
            p.Name,
            p.Status == ProjectStatus.Active ? "Aktiv"
                : p.Status == ProjectStatus.Completed ? "Fullfort"
                : p.Status == ProjectStatus.Planlagt ? "Planlagt"
                : "Kansellert",
            p.ClientName,
            p.StartDate,
            db.Deviations.Count(d => d.ProjectId == p.Id && d.Status != DeviationStatus.Closed),
            p.ParentProjectId,
            p.ParentProjectId != null ? db.Projects.Where(pp => pp.Id == p.ParentProjectId).Select(pp => pp.Name).FirstOrDefault() : null,
            db.Projects.Count(sub => sub.ParentProjectId == p.Id && !sub.IsDeleted),
            db.Projects.Count(sub => sub.ParentProjectId == p.Id && !sub.IsDeleted && sub.Status == ProjectStatus.Completed)))
        .ToListAsync(ct);

    return Results.Ok(new PagedResult<ProjectListItemDto>(items, totalCount, page, pageSize));
}).RequireAuthorization();

app.MapGet("/api/projects/{id:guid}", async (
    Guid id,
    SolodocDbContext db,
    ITenantProvider tenantProvider,
    CancellationToken ct) =>
{
    if (tenantProvider.TenantId is null)
        return Results.Unauthorized();

    var project = await db.Projects
        .Where(p => p.Id == id && p.TenantId == tenantProvider.TenantId.Value)
        .Select(p => new ProjectDetailDto(
            p.Id, p.Name, p.Description,
            p.Status == ProjectStatus.Active ? "Aktiv"
                : p.Status == ProjectStatus.Completed ? "Fullfort"
                : p.Status == ProjectStatus.Planlagt ? "Planlagt"
                : "Kansellert",
            p.ClientName, p.StartDate, p.Address,
            db.Deviations
                .Where(d => d.ProjectId == p.Id && d.Status != DeviationStatus.Closed)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DeviationListItemDto(
                    d.Id, d.Title,
                    p.Name,
                    d.Status == DeviationStatus.Open ? "Apen" : "Under behandling",
                    d.Severity == DeviationSeverity.Low ? "Lav"
                        : d.Severity == DeviationSeverity.Medium ? "Middels"
                        : "Hoy",
                    db.Persons.Where(per => per.Id == d.ReportedById).Select(per => per.FullName).FirstOrDefault() ?? "",
                    d.CreatedAt))
                .ToList(),
            p.ParentProjectId,
            p.ParentProjectId != null ? db.Projects.Where(pp => pp.Id == p.ParentProjectId).Select(pp => pp.Name).FirstOrDefault() : null,
            db.Projects
                .Where(sub => sub.ParentProjectId == p.Id && !sub.IsDeleted)
                .Select(sub => new SubProjectSummaryDto(
                    sub.Id, sub.Name,
                    sub.Status == ProjectStatus.Active ? "Aktiv"
                        : sub.Status == ProjectStatus.Completed ? "Fullfort"
                        : sub.Status == ProjectStatus.Planlagt ? "Planlagt"
                        : "Kansellert",
                    db.TimeEntries.Where(t => t.ProjectId == sub.Id && !t.IsDeleted).Sum(t => t.Hours),
                    db.Deviations.Count(d => d.ProjectId == sub.Id && !d.IsDeleted && d.Status != DeviationStatus.Closed),
                    db.ChecklistInstances.Count(c => c.ProjectId == sub.Id && !c.IsDeleted && (c.Status == ChecklistInstanceStatus.Submitted || c.Status == ChecklistInstanceStatus.Approved)),
                    db.ChecklistInstances.Count(c => c.ProjectId == sub.Id && !c.IsDeleted)))
                .ToList()))
        .FirstOrDefaultAsync(ct);

    return project is not null ? Results.Ok(project) : Results.NotFound();
}).RequireAuthorization();

// Deviations
app.MapGet("/api/deviations", async (
    ClaimsPrincipal user,
    SolodocDbContext db,
    CancellationToken ct,
    int page = 1,
    int pageSize = 10,
    string? search = null,
    string? sortBy = null,
    bool sortDesc = false,
    Guid? projectId = null,
    Guid? locationId = null) =>
{
    var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(personIdClaim, out var personId))
        return Results.Unauthorized();

    var membership = await db.TenantMemberships
        .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
        .FirstOrDefaultAsync(ct);

    if (membership is null)
        return Results.Ok(new PagedResult<DeviationListItemDto>([], 0, page, pageSize));

    var tenantId = membership.TenantId;

    var query = db.Deviations.Where(d => d.TenantId == tenantId);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.ToLowerInvariant();
        query = query.Where(d =>
            d.Title.ToLower().Contains(term) ||
            (d.Description != null && d.Description.ToLower().Contains(term)));
    }

    if (projectId.HasValue)
        query = query.Where(d => d.ProjectId == projectId.Value);

    if (locationId.HasValue)
        query = query.Where(d => d.LocationId == locationId.Value);

    var totalCount = await query.CountAsync(ct);

    query = sortBy?.ToLowerInvariant() switch
    {
        "title" => sortDesc ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
        "status" => sortDesc ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
        "severity" => sortDesc ? query.OrderByDescending(d => d.Severity) : query.OrderBy(d => d.Severity),
        "createdat" => sortDesc ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
        "projectname" => sortDesc
            ? query.OrderByDescending(d => db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Name).FirstOrDefault())
            : query.OrderBy(d => db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Name).FirstOrDefault()),
        _ => query.OrderByDescending(d => d.CreatedAt)
    };

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(d => new DeviationListItemDto(
            d.Id,
            d.Title,
            db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Name).FirstOrDefault(),
            d.Status == DeviationStatus.Open ? "Åpen"
                : d.Status == DeviationStatus.InProgress ? "Under behandling"
                : "Lukket",
            d.Severity == DeviationSeverity.Low ? "Lav"
                : d.Severity == DeviationSeverity.Medium ? "Middels"
                : "Høy",
            db.Persons.Where(p => p.Id == d.ReportedById).Select(p => p.FullName).FirstOrDefault() ?? "",
            d.CreatedAt))
        .ToListAsync(ct);

    return Results.Ok(new PagedResult<DeviationListItemDto>(items, totalCount, page, pageSize));
}).RequireAuthorization();

app.MapPost("/api/deviations", async (
    CreateDeviationRequest request,
    ClaimsPrincipal user,
    SolodocDbContext db,
    ITenantProvider tenantProvider,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { error = "Tittel er påkrevd." });

    var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(personIdClaim, out var personId) || tenantProvider.TenantId is null)
        return Results.Unauthorized();

    var severity = request.Severity switch
    {
        "Lav" => DeviationSeverity.Low,
        "Middels" => DeviationSeverity.Medium,
        "Høy" => DeviationSeverity.High,
        _ => DeviationSeverity.Medium
    };

    var deviation = new Solodoc.Domain.Entities.Deviations.Deviation
    {
        TenantId = tenantProvider.TenantId.Value,
        Title = request.Title,
        Description = request.Description,
        Severity = severity,
        ProjectId = request.ProjectId,
        JobId = request.JobId,
        LocationId = request.LocationId,
        CategoryId = request.CategoryId,
        ReportedById = personId,
        Status = DeviationStatus.Open
    };

    db.Deviations.Add(deviation);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/api/deviations/{deviation.Id}", new { id = deviation.Id });
}).RequireAuthorization();

app.MapGet("/api/deviations/{id:guid}", async (
    Guid id,
    SolodocDbContext db,
    ITenantProvider tenantProvider,
    CancellationToken ct) =>
{
    if (tenantProvider.TenantId is null) return Results.Unauthorized();

    var deviation = await db.Deviations
        .Where(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value)
        .Select(d => new DeviationDetailDto(
            d.Id, d.Title, d.Description,
            d.Status == DeviationStatus.Open ? "Åpen"
                : d.Status == DeviationStatus.InProgress ? "Under behandling" : "Lukket",
            d.Severity == DeviationSeverity.Low ? "Lav"
                : d.Severity == DeviationSeverity.Medium ? "Middels"
                : "Høy",
            d.CategoryId != null
                ? db.DeviationCategories.Where(c => c.Id == d.CategoryId).Select(c => c.Name).FirstOrDefault()
                : null,
            db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Name).FirstOrDefault(),
            d.ProjectId,
            db.Persons.Where(p => p.Id == d.ReportedById).Select(p => p.FullName).FirstOrDefault() ?? "",
            d.AssignedToId != null
                ? db.Persons.Where(p => p.Id == d.AssignedToId).Select(p => p.FullName).FirstOrDefault()
                : null,
            d.CreatedAt,
            d.ClosedAt,
            d.CorrectiveAction,
            d.CorrectiveActionDeadline,
            d.CorrectiveActionCompletedAt,
            db.DeviationComments
                .Where(c => c.DeviationId == d.Id)
                .OrderBy(c => c.PostedAt)
                .Select(c => new DeviationCommentDto(
                    c.Id,
                    db.Persons.Where(p => p.Id == c.AuthorId).Select(p => p.FullName).FirstOrDefault() ?? "",
                    c.Text,
                    c.PostedAt))
                .ToList()))
        .FirstOrDefaultAsync(ct);

    return deviation is not null ? Results.Ok(deviation) : Results.NotFound();
}).RequireAuthorization();

app.MapPut("/api/deviations/{id:guid}/status", async (
    Guid id,
    UpdateDeviationStatusRequest request,
    ClaimsPrincipal user,
    SolodocDbContext db,
    ITenantProvider tenantProvider,
    CancellationToken ct) =>
{
    if (tenantProvider.TenantId is null) return Results.Unauthorized();
    var deviation = await db.Deviations
        .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
    if (deviation is null) return Results.NotFound();

    var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    Guid.TryParse(personIdClaim, out var personId);

    deviation.Status = request.NewStatus switch
    {
        "Åpen" => DeviationStatus.Open,
        "Under behandling" => DeviationStatus.InProgress,
        "Lukket" => DeviationStatus.Closed,
        _ => deviation.Status
    };

    if (deviation.Status == DeviationStatus.Closed)
    {
        deviation.ClosedAt = DateTimeOffset.UtcNow;
        deviation.ClosedById = personId;
    }
    else
    {
        deviation.ClosedAt = null;
        deviation.ClosedById = null;
    }

    await db.SaveChangesAsync(ct);
    return Results.Ok();
}).RequireAuthorization();

// Deviation count (for sidebar badge)
app.MapGet("/api/deviations/open-count", async (
    ClaimsPrincipal user,
    SolodocDbContext db,
    CancellationToken ct) =>
{
    var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(personIdClaim, out var personId))
        return Results.Unauthorized();

    var membership = await db.TenantMemberships
        .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
        .FirstOrDefaultAsync(ct);

    if (membership is null)
        return Results.Ok(new { count = 0 });

    var count = await db.Deviations
        .CountAsync(d => d.TenantId == membership.TenantId && d.Status != DeviationStatus.Closed, ct);

    return Results.Ok(new { count });
}).RequireAuthorization();

// Invitation endpoints (public — no auth required)
app.MapGet("/api/invitations/{id:guid}", async (
    Guid id,
    SolodocDbContext db,
    CancellationToken ct) =>
{
    var invitation = await db.Invitations
        .IgnoreQueryFilters()
        .Where(i => i.Id == id)
        .Select(i => new InvitationDetailDto(
            i.Id,
            i.Email,
            i.State.ToString(),
            i.ExpiresAt,
            i.InvitedByName,
            db.Tenants.Where(t => t.Id == i.TenantId).Select(t => t.Name).FirstOrDefault() ?? "",
            i.IntendedRole == TenantRole.TenantAdmin ? "Administrator"
                : i.IntendedRole == TenantRole.ProjectLeader ? "Prosjektleder"
                : "Feltarbeider"))
        .FirstOrDefaultAsync(ct);

    if (invitation is null) return Results.NotFound();
    return Results.Ok(invitation);
}).AllowAnonymous();

app.MapPost("/api/invitations/{id:guid}/accept", async (
    Guid id,
    AcceptInvitationRequest request,
    SolodocDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    CancellationToken ct) =>
{
    var invitation = await db.Invitations
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(i => i.Id == id, ct);

    if (invitation is null)
        return Results.NotFound(new { error = "Invitasjonen finnes ikke." });

    if (invitation.State != InvitationState.Pending)
        return Results.BadRequest(new { error = "Invitasjonen er allerede brukt eller tilbakekalt." });

    if (invitation.ExpiresAt < DateTimeOffset.UtcNow)
    {
        invitation.State = InvitationState.Expired;
        await db.SaveChangesAsync(ct);
        return Results.BadRequest(new { error = "Invitasjonen har utl\u00f8pt." });
    }

    // Check if person already exists
    var emailLower = invitation.Email.ToLowerInvariant();
    var person = await db.Persons.FirstOrDefaultAsync(p => p.Email == emailLower, ct);

    if (person is null)
    {
        // Must create account
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Fullt navn og passord er p\u00e5krevd for nye brukere." });

        person = new Solodoc.Domain.Entities.Auth.Person
        {
            Email = invitation.Email.ToLowerInvariant(),
            FullName = request.FullName,
            PasswordHash = passwordHasher.Hash(request.Password),
            State = Solodoc.Domain.Enums.PersonState.Active,
            EmailVerified = true
        };
        db.Persons.Add(person);
    }

    // Check if already a member of this tenant
    var existingMembership = await db.TenantMemberships
        .IgnoreQueryFilters()
        .AnyAsync(m => m.PersonId == person.Id && m.TenantId == invitation.TenantId && m.State == TenantMembershipState.Active, ct);

    if (existingMembership)
        return Results.BadRequest(new { error = "Du er allerede medlem av denne organisasjonen." });

    // Create membership
    var membership = new Solodoc.Domain.Entities.Auth.TenantMembership
    {
        PersonId = person.Id,
        TenantId = invitation.TenantId,
        Role = invitation.IntendedRole,
        State = TenantMembershipState.Active
    };
    db.TenantMemberships.Add(membership);

    // Mark invitation as accepted
    invitation.State = InvitationState.Accepted;
    invitation.AcceptedAt = DateTimeOffset.UtcNow;
    invitation.AcceptedByPersonId = person.Id;

    await db.SaveChangesAsync(ct);

    // Generate tokens so user is logged in immediately
    var roleStr = invitation.IntendedRole switch
    {
        TenantRole.TenantAdmin => "tenant-admin",
        TenantRole.ProjectLeader => "project-leader",
        TenantRole.FieldWorker => "field-worker",
        _ => (string?)null
    };
    var accessToken = tokenService.GenerateAccessToken(person, invitation.TenantId, roleStr);
    var refreshTokenValue = tokenService.GenerateRefreshToken();

    var refreshToken = new Solodoc.Domain.Entities.Auth.RefreshToken
    {
        PersonId = person.Id,
        Token = refreshTokenValue,
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
    };
    db.RefreshTokens.Add(refreshToken);
    await db.SaveChangesAsync(ct);

    var authResponse = new AuthResponse(
        accessToken,
        refreshTokenValue,
        DateTimeOffset.UtcNow.AddMinutes(15),
        new PersonDto(person.Id, person.Email, person.FullName));

    return Results.Ok(new AcceptInvitationResponse(person.Email, invitation.TenantId, authResponse));
}).AllowAnonymous();

// Contact form (landing page — public, rate limited)
app.MapPost("/api/contact", async (
    ContactFormRequest request,
    IEmailService emailService,
    SolodocDbContext db,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new { error = "Navn og e-post er påkrevd." });

    // Always store the message (doesn't depend on SMTP)
    db.Feedbacks.Add(new Solodoc.Domain.Entities.Help.Feedback
    {
        Type = "contact",
        Message = $"Bedrift: {request.Company}\nNavn: {request.Name}\nTelefon: {request.Phone}\nE-post: {request.Email}\n\n{request.Message}",
    });
    await db.SaveChangesAsync(ct);

    // Try to send email (don't fail if SMTP not configured)
    try
    {
        var subject = $"Ny henvendelse fra {request.Company ?? "ukjent"}";
        var body = $"""
            Ny henvendelse fra solodoc.no

            Bedrift: {request.Company}
            Navn: {request.Name}
            Telefon: {request.Phone}
            E-post: {request.Email}

            Melding:
            {request.Message}
            """;
        await emailService.SendAsync("kontakt@solodoc.no", subject, body, ct);
    }
    catch { /* SMTP not configured — message still saved in DB */ }

    return Results.Ok(new { sent = true });
}).AllowAnonymous().RequireRateLimiting("auth");

app.MapDeviationEndpoints();
app.MapProjectEndpoints();
app.MapJobEndpoints();
app.MapCustomerEndpoints();
app.MapHoursEndpoints();
app.MapAbsenceEndpoints();
app.MapShiftEndpoints();
app.MapLocationEndpoints();
app.MapFileEndpoints();
app.MapCheckInEndpoints();
app.MapEmployeeEndpoints();
app.MapContactEndpoints();
app.MapCalendarEndpoints();
app.MapTaskGroupEndpoints();
app.MapChemicalEndpoints();
app.MapNotificationEndpoints();
app.MapSearchEndpoints();
app.MapChecklistEndpoints();
app.MapHmsEndpoints();
app.MapProcedureEndpoints();
app.MapEquipmentEndpoints();
app.MapExportEndpoints();
app.MapReportEndpoints();
app.MapForefallendeEndpoints();
app.MapRoleEndpoints();
app.MapOnboardingEndpoints();
app.MapChatbotEndpoints();
app.MapMarketplaceEndpoints();
app.MapSuperAdminEndpoints();
app.MapExpenseEndpoints();
app.MapTravelExpenseEndpoints();
app.MapExpenseSettingsEndpoints();
app.MapSubcontractorEndpoints();
app.MapProjectPostEndpoints();
app.MapCouponEndpoints();

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program;

public record ContactFormRequest(string? Company, string? Name, string? Phone, string? Email, string? Message);

