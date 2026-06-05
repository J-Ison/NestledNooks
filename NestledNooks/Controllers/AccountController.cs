using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NestledNooks.Data;

namespace NestledNooks.Controllers
{
    [Route("api/account")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Redirect("/login?error=InvalidLogin");

            try
            {
                var user = await _userManager.FindByEmailAsync(email.Trim()).ConfigureAwait(false);

                if (user is null)
                    return Redirect("/login?error=UserNotFound");

                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    password,
                    isPersistent: true,
                    lockoutOnFailure: false).ConfigureAwait(false);

                if (result.Succeeded)
                    return Redirect("/");

                return Redirect("/login?error=InvalidLogin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", email);
                return Redirect("/login?error=ServerError");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync().ConfigureAwait(false);
            return Redirect("/");
        }
    }
}
