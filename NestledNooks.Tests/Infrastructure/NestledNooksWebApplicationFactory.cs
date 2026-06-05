using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Infrastructure;

/// <summary>
/// Hosts the real NestledNooks app with SQLite and fake email for HTTP smoke tests.
/// </summary>
public sealed class NestledNooksWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;
    public FakeEmailService EmailService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Background calendar sync is not needed for page smoke tests.
            var calendarSync = services
                .Where(d => d.ImplementationType == typeof(CalendarSyncHostedService))
                .ToList();
            foreach (var descriptor in calendarSync)
                services.Remove(descriptor);

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            _connection ??= new SqliteConnection("DataSource=:memory:;Cache=Shared");
            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(EmailService);
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync().ConfigureAwait(false);

        await base.DisposeAsync().ConfigureAwait(false);
    }
}
