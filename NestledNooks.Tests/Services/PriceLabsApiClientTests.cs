using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class PriceLabsApiClientTests
{
    private const string SampleResponse = """
        [
          {
            "id": "1385672541118250169",
            "pms": "airbnb",
            "currency": "USD",
            "data": [
              { "date": "2026-06-24", "price": 312, "min_stay": 2 },
              { "date": "2026-06-25", "price": 318, "min_stay": 2 }
            ]
          }
        ]
        """;

    [Fact]
    public async Task GetListingPricesAsync_ParsesListingPricesPostResponse()
    {
        var handler = new FakePriceLabsHandler(SampleResponse);
        var client = CreateClient(handler);

        var prices = await client.GetListingPricesAsync(
            "1385672541118250169",
            "airbnb",
            new DateOnly(2026, 6, 24),
            new DateOnly(2026, 6, 26));

        Assert.Equal(2, prices.Count);
        Assert.Equal(312m, prices[0].Rate);
        Assert.Equal(2, prices[0].MinimumStay);
        Assert.Equal(new DateOnly(2026, 6, 25), prices[1].Date);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Contains("listing_prices", handler.LastUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"id\":\"1385672541118250169\"", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("\"pms\":\"airbnb\"", handler.LastBody, StringComparison.Ordinal);
    }

    private static PriceLabsApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new PriceLabsOptions { ApiKey = "test-key" }),
            NullLogger<PriceLabsApiClient>.Instance);

    private sealed class FakePriceLabsHandler(string responseBody) : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }
        public string? LastUrl { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastUrl = request.RequestUri?.ToString();
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
