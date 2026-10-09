using CakeStudio.Models;
using CakeStudio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CakeStudio.ViewModels;

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
            // Check whether user is already logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                // Redirect Baker to Cake Management
                if (User.IsInRole("Baker"))
                {
                    return RedirectToAction("Index", "Cake");
                }

                // Redirect Customer to Customer Cake Listing
                if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("Index", "CustomerCake");
                }
            }

            // Display Login page for unauthenticated users
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

            if (user.Role != "Baker" && user.Role != "Customer")
            {
                ModelState.AddModelError("", "Invalid account role.");
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

            // Redirect based on user role
            if (user.Role == "Baker")
            {
                return RedirectToAction("Index", "Cake");
            }
            else
            {
                return RedirectToAction("Index", "CustomerCake");
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Process Customer Registration form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            // Check form validation
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Convert RegisterViewModel into User model
            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address
            };

            // Call UserService to register customer
            var response = _userService.Register(user, model.Password);

            // Registration failed
            if (!response.Status)
            {
                ModelState.AddModelError("", response.Message);
                return View(model);
            }

            // Registration successful
            TempData["SuccessMessage"] = response.Message;

            return RedirectToAction("Login");
        }


        // Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {

            // Remove authentication cookie
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