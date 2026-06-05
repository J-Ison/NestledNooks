using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Infrastructure;

/// <summary>
/// Builds an isolated in-memory SQLite database with Identity and app services for integration tests.
/// </summary>
public sealed class TestServiceScope : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Db { get; }
    public UserManager<ApplicationUser> UserManager { get; }
    public FakeEmailService EmailService { get; }
    public IContactInquiryService ContactInquiries { get; }

    private TestServiceScope(
        ServiceProvider serviceProvider,
        SqliteConnection connection,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        FakeEmailService emailService,
        IContactInquiryService contactInquiries)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
        Db = db;
        UserManager = userManager;
        EmailService = emailService;
        ContactInquiries = contactInquiries;
    }

    public static async Task<TestServiceScope> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var emailService = new FakeEmailService();
        services.AddSingleton<IEmailService>(emailService);
        services.AddScoped<IContactInquiryService, ContactInquiryService>();

        var serviceProvider = services.BuildServiceProvider();

        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var contactInquiries = serviceProvider.GetRequiredService<IContactInquiryService>();

        return new TestServiceScope(
            serviceProvider,
            connection,
            db,
            userManager,
            emailService,
            contactInquiries);
    }

    /// <summary>Creates a user with optional nickname for verified contact-inquiry tests.</summary>
    public async Task<ApplicationUser> CreateUserAsync(
        string email,
        string? nickname = null,
        string password = "TestPass123!")
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Nickname = nickname,
            PhoneNumber = "5555550100",
        };

        var result = await UserManager.CreateAsync(user, password).ConfigureAwait(false);
        Assert.True(
            result.Succeeded,
            $"Test user '{email}' should be created, but Identity returned: {string.Join("; ", result.Errors.Select(e => e.Description))}.");

        return user;
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync().ConfigureAwait(false);
        await _serviceProvider.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
