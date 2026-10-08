using CakeStudio.Models;
using CakeStudio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CakeStudio.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        // Display Login page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Process Login form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = _userService.Login(model.Email, model.Password);

            if (!response.Status || response.Data is not User user)
            {
                ModelState.AddModelError("", response.Message);
                return View(model);
            }

            // Only Baker accounts may sign in through this page.
            if (user.Role != "Baker")
            {
                ModelState.AddModelError("", "This login is for bakers only.");
                return View(model);
            }

            // Store user information as claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            // Create authentication cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction("Index", "Cake");
        }

        // Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }

        // Access denied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content("Access denied.");
        }
    }
}