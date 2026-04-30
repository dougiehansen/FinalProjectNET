using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Auth;
using PropertyManagement.Components;
using PropertyManagement.Data;
using PropertyManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddCookie("ExternalCookie", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGoogle(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    })
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
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthStateProvider>();

builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("ecb", c =>
{
    c.BaseAddress = new Uri("https://data-api.ecb.europa.eu/");
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("eurostat", c =>
{
    c.BaseAddress = new Uri("https://ec.europa.eu/");
    c.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<ILeaseService, LeaseService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<IRentPaymentService, RentPaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.InitializeAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

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
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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

app.Run();
