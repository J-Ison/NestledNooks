using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class PropertyOperationsServiceTests
{
    [Fact]
    public async Task SaveEquipmentAndContact_PersistsForHostStaff()
    {
        await using var scope = await CreateScopeAsync(AppRoles.Owner);

        var saveEquipment = await scope.Service.SaveEquipmentItemAsync(PropertySeedData.DeerfieldSlug, new PropertyEquipmentItemModel
        {
            Item = "Lockbox",
            Value = "2348",
        });

        Assert.True(saveEquipment.Success, saveEquipment.ErrorMessage);

        var saveContact = await scope.Service.SaveContactAsync(PropertySeedData.DeerfieldSlug, new PropertyContactModel
        {
            Name = "Joe Plumber",
            Role = "Plumber",
            Phone = "605-555-0100",
        });

        Assert.True(saveContact.Success, saveContact.ErrorMessage);

        var page = await scope.Service.GetPageDataAsync(PropertySeedData.DeerfieldSlug);
        Assert.NotNull(page);
        Assert.Single(page!.EquipmentItems);
        Assert.Equal("Lockbox", page.EquipmentItems[0].Item);
        Assert.Equal("2348", page.EquipmentItems[0].Value);
        Assert.Single(page.Contacts);
        Assert.Equal("Joe Plumber", page.Contacts[0].Name);
    }

    [Fact]
    public async Task GetPageDataAsync_RejectsClientRole()
    {
        await using var scope = await CreateScopeAsync(AppRoles.Client);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => scope.Service.GetPageDataAsync(PropertySeedData.DeerfieldSlug));
    }

    [Fact]
    public async Task SaveEquipmentItem_AllowsBlankItemAndValue()
    {
        await using var scope = await CreateScopeAsync(AppRoles.Manager);

        var result = await scope.Service.SaveEquipmentItemAsync(PropertySeedData.DeerfieldSlug, new PropertyEquipmentItemModel());
        Assert.True(result.Success, result.ErrorMessage);

        var page = await scope.Service.GetPageDataAsync(PropertySeedData.DeerfieldSlug);
        Assert.NotNull(page);
        Assert.Single(page!.EquipmentItems);
        Assert.Null(page.EquipmentItems[0].Item);
        Assert.Null(page.EquipmentItems[0].Value);
    }

    [Fact]
    public async Task SaveCustomField_AllowsBlankLabelAndValue()
    {
        await using var scope = await CreateScopeAsync(AppRoles.Manager);

        var result = await scope.Service.SaveCustomFieldAsync(PropertySeedData.DeerfieldSlug, new PropertyCustomFieldModel());
        Assert.True(result.Success, result.ErrorMessage);

        var page = await scope.Service.GetPageDataAsync(PropertySeedData.DeerfieldSlug);
        Assert.NotNull(page);
        Assert.Single(page!.CustomFields);
        Assert.Null(page.CustomFields[0].Label);
        Assert.Null(page.CustomFields[0].Value);
    }

    private static async Task<PropertyOperationsTestScope> CreateScopeAsync(string role)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var services = new ServiceCollection();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, role)],
                authenticationType: "test")),
        };
        services.AddSingleton<IHttpContextAccessor>(new TestHttpContextAccessor(httpContext));
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IPropertyOperationsService, PropertyOperationsService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
        db.RentalProperties.Add(PropertySeedData.CreateDeerfieldRetreat());
        await db.SaveChangesAsync().ConfigureAwait(false);

        return new PropertyOperationsTestScope(provider, scope, connection);
    }

    /// <summary>Plain HttpContext holder for tests (production accessor uses AsyncLocal).</summary>
    private sealed class TestHttpContextAccessor(HttpContext httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }

    private sealed class PropertyOperationsTestScope : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;
        private readonly SqliteConnection _connection;

        public PropertyOperationsTestScope(ServiceProvider provider, IServiceScope scope, SqliteConnection connection)
        {
            _provider = provider;
            _scope = scope;
            _connection = connection;
            Service = scope.ServiceProvider.GetRequiredService<IPropertyOperationsService>();
        }

        public IPropertyOperationsService Service { get; }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
