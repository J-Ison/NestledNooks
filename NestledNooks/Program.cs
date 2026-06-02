using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddScoped<BookingPricingService>();
builder.Services.AddScoped<IBookingAvailabilityService, BookingAvailabilityService>();
builder.Services.AddScoped<IBookingRequestService, BookingRequestService>();
builder.Services.AddScoped<BookingIcalExportService>();
builder.Services.AddHostedService<CalendarSyncHostedService>();

builder.Services.AddMudServices();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("CalendarSync", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("NestledNooks/1.0 (calendar-sync)");
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await SeedApplicationRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
    try
    {
        await scope.ServiceProvider.GetRequiredService<IBookingAvailabilityService>()
            .SyncExternalCalendarsAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogWarning(ex, "Initial calendar sync skipped (configure Booking:Properties iCal URLs).");
    }
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

app.UseHttpsRedirection();
app.MapControllers();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles();

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
