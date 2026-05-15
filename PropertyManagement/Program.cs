// ── Imports ──────────────────────────────────────────────────────────────────
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Auth;
using PropertyManagement.Components;
using PropertyManagement.Data;
using PropertyManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// connects to sqlite using the connection string from appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Authentication ────────────────────────────────────────────────────────────
// main cookie auth - stores the login session, expires after 8 hours
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    // temporary cookie used during google/microsoft login flow, expires in 10 mins
    .AddCookie("ExternalCookie", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    // google social login - client id and secret stored in appsettings/secrets
    .AddGoogle(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    })
    // microsoft social login
    .AddMicrosoftAccount(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
        options.AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        options.TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
// my custom auth state provider that reads the login cookie and tells blazor who is logged in
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthStateProvider>();

// ── Blazor / Razor ────────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── External HTTP Clients ─────────────────────────────────────────────────────
// named http client for calling the ECB (European Central Bank) data API
builder.Services.AddHttpClient("ecb", c =>
{
    c.BaseAddress = new Uri("https://data-api.ecb.europa.eu/");
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});
// named http client for calling the Eurostat API
builder.Services.AddHttpClient("eurostat", c =>
{
    c.BaseAddress = new Uri("https://ec.europa.eu/");
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── My Services ───────────────────────────────────────────────────────────────
// registering all my feature services so they can be injected into pages
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<ILeaseService, LeaseService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IRentPaymentService, RentPaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPropertyAccessService, PropertyAccessService>();
builder.Services.AddScoped<BankStatementParser>();
builder.Services.AddScoped<StatementMatcherService>();
// singleton so all users share the same notification hub
builder.Services.AddSingleton<LeaseNotificationService>();
// background worker that checks for expiring leases
builder.Services.AddHostedService<LeaseExpiryWorker>();

// ── Build App ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// makes sure the folder for the sqlite db file exists before trying to connect
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
if (connStr.Contains("Data Source="))
{
    var dataSource = connStr.Split(new[] { "Data Source=" }, StringSplitOptions.None).Last().Split(';').First().Trim();
    if (Path.IsPathRooted(dataSource))
        Directory.CreateDirectory(Path.GetDirectoryName(dataSource)!);
}

// ── Database Seed ─────────────────────────────────────────────────────────────
// runs on startup to create tables and seed default data if the db is empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// ── Error Handling ────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// ── Proxy / Codespaces Fix ────────────────────────────────────────────────────
// when running in github codespaces the url needs to be overridden so oauth redirects work
var codespaceName = Environment.GetEnvironmentVariable("CODESPACE_NAME");
var portForwardingDomain = Environment.GetEnvironmentVariable("GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN") ?? "app.github.dev";

if (!string.IsNullOrEmpty(codespaceName))
{
    app.Use(async (context, next) =>
    {
        context.Request.Scheme = "https";
        context.Request.Host = new HostString($"{codespaceName}-5163.{portForwardingDomain}");
        await next(context);
    });
}
else
{
    // in production, trust forwarded headers from the reverse proxy (nginx etc)
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ── Route Mapping ─────────────────────────────────────────────────────────────
app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── API Proxy Endpoints ───────────────────────────────────────────────────────
// proxies ECB API calls through the server so i dont get CORS errors from the browser
app.MapGet("/api/ecb/{**path}", async (string path, HttpRequest req, IHttpClientFactory factory) =>
{
    var http = factory.CreateClient("ecb");
    var query = req.QueryString.Value ?? "";
    try
    {
        var res = await http.GetAsync($"service/data/{path}{query}");
        var body = await res.Content.ReadAsStringAsync();
        return Results.Content(body, "application/json");
    }
    catch
    {
        return Results.StatusCode(502);
    }
}).RequireAuthorization();

// proxies Eurostat API calls through the server for the same reason
app.MapGet("/api/eurostat/{**path}", async (string path, HttpRequest req, IHttpClientFactory factory) =>
{
    var http = factory.CreateClient("eurostat");
    var query = req.QueryString.Value ?? "";
    try
    {
        var res = await http.GetAsync($"eurostat/api/dissemination/{path}{query}");
        var body = await res.Content.ReadAsStringAsync();
        return Results.Content(body, "application/json");
    }
    catch
    {
        return Results.StatusCode(502);
    }
}).RequireAuthorization();

// returns the signed lease as an HTML document for viewing in the browser
app.MapGet("/api/lease/{id:int}/document", async (int id, ApplicationDbContext db) =>
{
    var lease = await db.Leases
        .Include(l => l.Tenant)
        .Include(l => l.Unit).ThenInclude(u => u.Property)
        .FirstOrDefaultAsync(l => l.Id == id);
    if (lease is null) return Results.NotFound();
    var html = PropertyManagement.Services.DocumentService.GenerateLeaseDocument(lease, signed: true);
    return Results.Content(html, "text/html");
}).RequireAuthorization();

app.Run();
