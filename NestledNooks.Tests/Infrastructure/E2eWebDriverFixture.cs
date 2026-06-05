using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace NestledNooks.Tests.Infrastructure;

/// <summary>
/// Shared Chrome session for E2E tests. WebDriverManager downloads a ChromeDriver
/// that matches the installed Chrome version.
/// </summary>
public sealed class E2eWebDriverFixture : IDisposable
{
    public IWebDriver Driver { get; }
    public string BaseUrl { get; }

    public E2eWebDriverFixture()
    {
        BaseUrl = E2eTestOptions.ResolveBaseUrl();

        var options = new ChromeOptions();
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--disable-gpu");

#if !DEBUG
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
#else
        options.AddArgument("--start-maximized");
#endif

        try
        {
            // Match ChromeDriver to installed Chrome (not "Latest" which can be one major version ahead).
            new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

            Driver = new ChromeDriver(options);
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
            Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not start Chrome for E2E tests. Install Google Chrome and ensure you can reach the test site. " +
                $"Target base URL: {BaseUrl}. Inner error: {ex.Message}",
                ex);
        }
    }

    public void Navigate(string relativePath)
    {
        var path = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        Driver.Navigate().GoToUrl(BaseUrl.TrimEnd('/') + path);
    }

    public IWebElement WaitFor(Func<IWebDriver, IWebElement?> condition, int timeoutSeconds = 20, string? because = null)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
        {
            Message = because ?? "Timed out waiting for the expected element on the page.",
        };

        return wait.Until(condition)
            ?? throw new InvalidOperationException(because ?? "Expected element was not found.");
    }

    public void WaitUntil(Func<IWebDriver, bool> condition, int timeoutSeconds = 20, string? because = null)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds))
        {
            Message = because ?? "Timed out waiting for page condition.",
        };

        wait.Until(condition);
    }

    public void Dispose()
    {
        try
        {
            Driver.Quit();
        }
        catch
        {
            // Best-effort cleanup when the browser was closed manually.
        }

        Driver.Dispose();
    }
}

/// <summary>
/// Target site for browser tests. Override with NESTLEDNOOKS_E2E_BASE_URL when testing locally.
/// </summary>
public static class E2eTestOptions
{
    public const string DefaultAzureBaseUrl =
        "https://nestlednooks-bvd3htchb9hwhzex.centralus-01.azurewebsites.net/";

    public static string ResolveBaseUrl()
    {
        var fromEnv = Environment.GetEnvironmentVariable("NESTLEDNOOKS_E2E_BASE_URL");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.TrimEnd('/') + "/";

        return DefaultAzureBaseUrl;
    }
}
