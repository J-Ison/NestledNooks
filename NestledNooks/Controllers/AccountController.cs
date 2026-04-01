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

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            Console.WriteLine($"[LOGIN] Attempt: {email}");

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                Console.WriteLine("[LOGIN] User not found");
                return Redirect("/login?error=UserNotFound");
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                password,
                isPersistent: true,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                Console.WriteLine("[LOGIN] Success");
                return Redirect("/");
            }

            Console.WriteLine("[LOGIN] Invalid password");
            return Redirect("/login?error=InvalidLogin");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}
