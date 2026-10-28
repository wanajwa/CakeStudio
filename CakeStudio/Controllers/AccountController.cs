using CakeStudio.Models;
using CakeStudio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CakeStudio.ViewModels;
using CakeStudio.Resources;

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
                ModelState.AddModelError("", Resource.InvalidAccountRole);
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
            var user = new Customer
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                Address = string.IsNullOrWhiteSpace(model.Address)
                    ? null
                    : model.Address.Trim()
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

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Profile()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out int customerId))
                return Challenge();

            var response = _userService.GetCustomerProfile(customerId);
            if (!response.Status || response.Data is not CustomerProfileViewModel model)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("Index", "CustomerCake");

            }
            return View(model);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CustomerProfileViewModel model)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out int customerId))
                return Challenge();

            var profileResponse = _userService.GetCustomerProfile(customerId);
            if (!profileResponse.Status || profileResponse.Data is not CustomerProfileViewModel profile)
            {
                TempData["ErrorMessage"] = profileResponse.Message;
                return RedirectToAction("Index", "CustomerCake");
            }

            model.Email = profile.Email;
            ModelState.Remove(nameof(model.Email));

            if(!ModelState.IsValid)
                return View(model);
            var response = _userService.UpdateCustomerProfile(customerId,model);

            if (!response.Status)
            {
                ModelState.AddModelError("",response.Message);
                return View(model);
            }

            var authentication = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var identity = new ClaimsIdentity((ClaimsIdentity)User.Identity!);
            var nameClaim = identity.FindFirst(ClaimTypes.Name);

            if (nameClaim != null) 
                identity.RemoveClaim(nameClaim);
            identity.AddClaim(new Claim(ClaimTypes.Name, model.Name.Trim()));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity), authentication.Properties);

            TempData["SuccessMessage"] = response.Message;

            return RedirectToAction(nameof(Profile));

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
            return Content(Resource.AccessDenied);
        }
    }

}