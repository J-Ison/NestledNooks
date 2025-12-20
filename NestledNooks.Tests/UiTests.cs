using System;
using Xunit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace NestledNooks.UiTests;

public class UiTests : IDisposable
{
    private readonly IWebDriver _driver;
    //private const string BaseUrl = "https://localhost:7225/"; // local
    private const string BaseUrl = "https://nestlednooks-bvd3htchb9hwhzex.centralus-01.azurewebsites.net/"; // deployed site

    public UiTests()
    {
        var options = new ChromeOptions();

#if !DEBUG
        // CI / Release: run headless (no display) and use container-safe flags
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");w
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
#else
        // Local / Debug: show real browser for debugging
        options.AddArgument("--start-maximized");
#endif

        // Optional but nice for consistency
        options.AddArgument("--window-size=1920,1080");

        _driver = new ChromeDriver(options);
    }

    public void Dispose()
    {
        _driver.Quit();
    }

    [Fact]
    public void DeerfieldRetreatProperty_Loads_AndShowsPropertyTitle()
    {
        _driver.Navigate().GoToUrl(BaseUrl);

        Assert.Contains("Deerfield Retreat", _driver.Title + _driver.PageSource);
    }

    public void DeerfieldRetreatProperty_HasAirbnbAndVrboButtons()
    {
        _driver.Navigate().GoToUrl(BaseUrl);

        // These are <a> tags styled as buttons, so LinkText should work
        var airbnbButton = _driver.FindElement(By.LinkText("View availability on Airbnb"));
        var vrboButton = _driver.FindElement(By.LinkText("View availability on Vrbo"));

        Assert.NotNull(airbnbButton);
        Assert.NotNull(vrboButton);

        // Basic sanity check that they actually go somewhere
        Assert.False(string.IsNullOrWhiteSpace(airbnbButton.GetAttribute("href")));
        Assert.False(string.IsNullOrWhiteSpace(vrboButton.GetAttribute("href")));
    }


    [Fact]
    public void Smoke_ContactPageLoads()
    {
        _driver.Navigate().GoToUrl($@"{BaseUrl}contact");
        var h1 = WaitForElement(By.TagName("h1"));
        Assert.Equal("Contact Us", h1.Text);
    }

    [Fact]
    public void Smoke_LoginPageLoads()
    {
        _driver.Navigate().GoToUrl($@"{BaseUrl}login");
        var loginButton = WaitForElement(By.CssSelector("button.btn-login"));
        Assert.Equal("Login", loginButton.Text);
    }

    [Fact]
    public void Smoke_RegisterPageLoads()
    {
        _driver.Navigate().GoToUrl($@"{BaseUrl}Register");
        var createAccountBtn = WaitForElement(By.CssSelector("button.btn-login"));
        Assert.Equal("Create Account", createAccountBtn.Text.Trim());
    }

    public void Smoke_DeerfieldRetreatPropertyPageLoads()
    {
        _driver.Navigate().GoToUrl($@"{BaseUrl}");
        var title = WaitForElement(By.ClassName("title"));
        Assert.Equal("Deerfield Retreat", title.Text.Trim());
    }

    private IWebElement WaitForElement(By by, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(new SystemClock(), _driver,
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeSpan.FromMilliseconds(500));

        return wait.Until(drv => drv.FindElement(by));
    }
}
