using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EqualFutures.Core;
using EqualFutures.Infrastructure;
using EqualFutures.Infrastructure.Data;
using EqualFutures.Web.Components;
using EqualFutures.Web.Components.Account;
using EqualFutures.Web.Data;
using EqualFutures.Web.Services;
using EqualFutures.Web.Services.Email;
using EqualFutures.Web.Services.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = ResolveSqliteConnectionString(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// EqualFutures financial planning services.
builder.Services.AddEqualFuturesCore();
builder.Services.AddEqualFuturesInfrastructure(connectionString);
builder.Services.AddScoped<PlanState>();

// Surfaces Warning-and-above logs in the admin portal for diagnostics.
builder.Services.AddSingleton<ILoggerProvider, DatabaseLoggerProvider>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 1;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Email delivery via the Gmail-backed Azure Logic App.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddHttpClient(LogicAppEmailSender.HttpClientName);
builder.Services.AddTransient<IAppEmailSender, LogicAppEmailSender>();
builder.Services.AddTransient<IEmailSender<ApplicationUser>, IdentityEmailSender>();

var app = builder.Build();

// Apply database migrations for both the Identity and financial contexts.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    await services.GetRequiredService<FinancialDbContext>().Database.MigrateAsync();
    await EnsureAdminRoleAsync(services, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

/// <summary>
/// Ensures the "Admin" role exists and is granted to every email listed under the
/// "Admin:Emails" configuration section. This is the only way to grant admin access —
/// there's no in-app "make admin" button, by design.
/// </summary>
static async Task EnsureAdminRoleAsync(IServiceProvider services, IConfiguration configuration)
{
    const string adminRole = "Admin";

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRole));
    }

    var adminEmails = configuration.GetSection("Admin:Emails").Get<string[]>() ?? [];
    if (adminEmails.Length == 0) return;

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var email in adminEmails)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) continue;

        if (!await userManager.IsInRoleAsync(user, adminRole))
        {
            await userManager.AddToRoleAsync(user, adminRole);
        }
    }
}

static string ResolveSqliteConnectionString(string connectionString)
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
    {
        return connectionString;
    }

    var builder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(builder.DataSource) || Path.IsPathRooted(builder.DataSource))
    {
        return connectionString;
    }

    var homeDirectory = Environment.GetEnvironmentVariable("HOME");
    if (string.IsNullOrWhiteSpace(homeDirectory))
    {
        return connectionString;
    }

    var dataDirectory = Path.Combine(homeDirectory, "data");
    Directory.CreateDirectory(dataDirectory);

    builder.DataSource = Path.Combine(dataDirectory, Path.GetFileName(builder.DataSource));
    return builder.ToString();
}
