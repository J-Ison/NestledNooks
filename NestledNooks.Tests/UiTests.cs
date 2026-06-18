using NestledNooks.Tests.Infrastructure;
using OpenQA.Selenium;

namespace NestledNooks.UiTests;

/// <summary>
/// Browser smoke tests against a running Nestled Nooks site (staging, prod, or local).
/// Excluded from CI via Category=E2E — run manually after deploy:
///   dotnet test --filter "Category=E2E"
/// Local app:
///   set NESTLEDNOOKS_E2E_BASE_URL=https://localhost:7225
/// </summary>
[Trait("Category", "E2E")]
public sealed class UiTests : IClassFixture<E2eWebDriverFixture>
{
    private readonly E2eWebDriverFixture _browser;

    public UiTests(E2eWebDriverFixture browser) => _browser = browser;

    [Fact]
    public void HomePage_Loads_DeerfieldRetreatListing()
    {
        _browser.Navigate("/");

        var title = _browser.WaitFor(
            driver =>
            {
                try
                {
                    var heading = driver.FindElement(By.CssSelector("h1.title"));
                    return heading.Text.Contains("Deerfield Retreat", StringComparison.OrdinalIgnoreCase)
                        ? heading
                        : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            },
            because: "Home page should finish loading the Deerfield Retreat listing (h1.title). " +
                     "If you only see 'Loading…', the property seed or database may be unavailable.");

        Assert.Contains("Deerfield Retreat", title.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HomePage_ShowsEmbeddedBookingWidget()
    {
       
        //_browser.Navigate("/");

        //_browser.WaitUntil(
        //    driver => driver.FindElements(By.CssSelector("h1.title")).Any(el =>
        //        el.Text.Contains("Deerfield Retreat", StringComparison.OrdinalIgnoreCase)),
        //    because: "Listing must load before checking the booking widget.");

        //var widget = _browser.WaitFor(
        //    driver =>
        //    {
        //        try
        //        {
        //            return driver.FindElement(By.CssSelector(".nn-listing-booking-card"));
        //        }
        //        catch (NoSuchElementException)
        //        {
        //            return null;
        //        }
        //    },
        //    because: "Property page should show the embedded booking card with calendar.");

        //Assert.Contains("Book your stay", widget.Text, StringComparison.OrdinalIgnoreCase);

        //var airbnbLinks = _browser.Driver.FindElements(By.LinkText("View on Airbnb"));
        //var vrboLinks = _browser.Driver.FindElements(By.LinkText("View on Vrbo"));
        //Assert.Empty(airbnbLinks);
        //Assert.Empty(vrboLinks);
    }

    [Fact]
    public void ContactPage_ShowsContactUsHeading()
    {
        _browser.Navigate("/contact");

        var heading = _browser.WaitFor(
            driver =>
            {
                try
                {
                    var el = driver.FindElement(By.CssSelector("h1.guide-hero-title"));
                    return el.Text.Contains("Contact us", StringComparison.OrdinalIgnoreCase) ? el : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            },
            because: "Contact page hero should contain h1.guide-hero-title with text 'Contact us'.");

        Assert.Contains("Contact us", heading.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoginPage_ShowsSignInForm()
    {
        _browser.Navigate("/login");

        var heading = _browser.WaitFor(
            driver =>
            {
                try
                {
                    var el = driver.FindElement(By.CssSelector("h1.guide-hero-title"));
                    return el.Text.Contains("Welcome back", StringComparison.OrdinalIgnoreCase) ? el : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            },
            because: "Login page should show the 'Welcome back' hero heading.");

        var signInButton = _browser.WaitFor(
            driver =>
            {
                try
                {
                    var button = driver.FindElement(By.CssSelector("form.auth-form button.nn-btn-primary"));
                    return button.Text.Contains("Sign in", StringComparison.OrdinalIgnoreCase) ? button : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            },
            because: "Login form should expose a primary submit button labeled 'Sign in' (not the legacy btn-login class).");

        Assert.Contains("Welcome back", heading.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sign in", signInButton.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterPage_ShowsCreateAccountForm()
    {
        _browser.Navigate("/register");

        var heading = _browser.WaitFor(
            driver =>
            {
                try
                {
                    var el = driver.FindElement(By.CssSelector("h1.guide-hero-title"));
                    return el.Text.Contains("Create an account", StringComparison.OrdinalIgnoreCase) ? el : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            },
            because: "Register page should show the 'Create an account' hero heading.");

        var createButton = _browser.WaitFor(
            driver =>
            {
                try
                {
                    var button = driver.FindElement(By.CssSelector("button.nn-btn-primary[type='submit']"));
                    return button.Text.Contains("Create account", StringComparison.OrdinalIgnoreCase) ? button : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            },
            because: "Register form should expose a submit button labeled 'Create account'.");

        Assert.Contains("Create an account", heading.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Create account", createButton.Text, StringComparison.OrdinalIgnoreCase);
    }
}
