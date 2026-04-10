using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Solodoc.Client;
using Solodoc.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// In production, Client and API are behind the same Caddy reverse proxy (same origin).
// In development, API runs on a separate port.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? (builder.HostEnvironment.IsDevelopment() ? "http://localhost:5078" : builder.HostEnvironment.BaseAddress);

// Plain HttpClient for unauthenticated calls (login, register)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

// Authenticated wrapper for API calls (handles 401 → redirect to login)
builder.Services.AddScoped(sp =>
{
    var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var navigation = sp.GetRequiredService<NavigationManager>();
    return new ApiHttpClient(http, localStorage, navigation);
});

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();

builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<AuthHttpService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<DeviationService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<HoursService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<ChecklistService>();
builder.Services.AddScoped<HmsService>();
builder.Services.AddScoped<ChemicalService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ProcedureService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ForefallendeService>();
builder.Services.AddScoped<ShiftService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<CheckInService>();
builder.Services.AddScoped<OfflineStorageService>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddScoped<OfflineAwareApiClient>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
