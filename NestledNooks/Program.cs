using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using NestledNooks.Components;
using NestledNooks.Components.Account;
using NestledNooks.Data;
using NestledNooks.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddControllers();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<HttpClient>(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigation.BaseUri)
    };
});

// Identity email sender (used by account pages)
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Smtp email sender for site contact form
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection(BookingOptions.SectionName));
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.AddScoped<BookingPricingService>();
builder.Services.AddScoped<IBookingAvailabilityService, BookingAvailabilityService>();
builder.Services.AddScoped<IBookingRequestService, BookingRequestService>();
builder.Services.AddScoped<BookingIcalExportService>();
builder.Services.AddSingleton<SiteThemeCache>();
builder.Services.AddScoped<ISiteThemePreviewAccessor, SiteThemePreviewAccessor>();
builder.Services.AddScoped<ISiteThemeService, SiteThemeService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IContactInquiryService, ContactInquiryService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHostedService<CalendarSyncHostedService>();

builder.Services.AddMudServices();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("CalendarSync", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("NestledNooks/1.0 (calendar-sync)");
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using (var migrateScope = app.Services.CreateAsyncScope())
    {
        var migrateLogger = migrateScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        var db = migrateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await db.Database.MigrateAsync().ConfigureAwait(false);
            migrateLogger.LogInformation("Database migrations applied.");
        }
        catch (Exception ex)
        {
            migrateLogger.LogCritical(
                ex,
                "Database migration failed. Will attempt direct schema repair for login-critical columns.");
        }

        try
        {
            await DatabaseSchemaRepair.EnsureAllAsync(db, migrateLogger).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            migrateLogger.LogCritical(
                ex,
                "Database schema repair failed. Sign-in and messaging may not work until migrations are applied.");
        }
    }
}

if (!app.Environment.IsDevelopment()
    && connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
{
    app.Logger.LogWarning(
        "DefaultConnection uses LocalDB, which does not run on Azure App Service. " +
        "Set ConnectionStrings__DefaultConnection in Application settings to your Azure SQL connection string.");
}

// Do not block HTTP startup on SQL or iCal — Azure health checks fail after ~230s (ContainerTimeout).
_ = Task.Run(async () =>
{
    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

        try
        {
            await SeedApplicationRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>())
                .ConfigureAwait(false);

            await OwnerRoleSeedService.EnsureRoleAssignmentsAsync(
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>(),
                logger).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Role seed skipped. Verify ConnectionStrings:DefaultConnection and that migrations are applied on the database.");
        }

        try
        {
            await scope.ServiceProvider.GetRequiredService<IPropertyService>()
                .EnsureSeededAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Property seed skipped.");
        }

        try
        {
            await scope.ServiceProvider.GetRequiredService<ISiteThemeService>()
                .EnsureSeededAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Site theme seed skipped.");
        }

        try
        {
            await scope.ServiceProvider.GetRequiredService<IQrCodeService>()
                .EnsureSeededAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Site QR settings seed skipped.");
        }

        try
        {
            await scope.ServiceProvider.GetRequiredService<IBookingAvailabilityService>()
                .SyncExternalCalendarsAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Initial calendar sync skipped.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Background startup tasks failed.");
    }
});

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

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

static async Task SeedApplicationRolesAsync(RoleManager<IdentityRole> roleManager)
{
    foreach (var roleName in new[] { AppRoles.Owner, AppRoles.CoHost, AppRoles.Manager, AppRoles.Client })
    {
        if (await roleManager.RoleExistsAsync(roleName))
            continue;

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}

public record LoginRequest(string Email, string Password);

public partial class Program;
