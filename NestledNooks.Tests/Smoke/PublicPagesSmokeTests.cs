using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using NestledNooks.Tests.Infrastructure;

namespace NestledNooks.Tests.Smoke;

/// <summary>
/// Lightweight HTTP smoke tests against the real app pipeline (Blazor + middleware).
/// Uses SQLite in memory — no Azure or LocalDB required in CI.
/// </summary>
public sealed class PublicPagesSmokeTests : IClassFixture<NestledNooksWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PublicPagesSmokeTests(NestledNooksWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
        });
    }

    [Fact]
    public async Task HomePage_ReturnsSuccessAndMentionsDeerfieldRetreat()
    {
        var response = await _client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Deerfield Retreat", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ContactPage_ReturnsSuccessAndShowsContactForm()
    {
        var response = await _client.GetAsync("/contact");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Contact us", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPage_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
