using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EduConnect.Models;

namespace EduConnect.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        // Whitelist of allowed student emails
        private static readonly HashSet<string> AllowedStudentEmails = new()
        {
            "22X01A6748@nrcmec.org",
            "22X01A6647@nrcmec.org",
            "22X01A6761@nrcmec.org",
            "22X01A6751@nrcmec.org",
            "22X01A6762@nrcmec.org"
        };

        // Only allowed faculty email
        private static readonly string AllowedFacultyEmail = "RamuGandikota@gmail.com";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Validate email against whitelist
                if (!AllowedStudentEmails.Contains(model.Email ?? "") && model.Email != AllowedFacultyEmail && model.Email != "admin@educonnect.com")
                {
                    ModelState.AddModelError(string.Empty, "❌ Access Denied: Your email is not authorized to access this system.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email ?? "", 
                    model.Password ?? "", 
                    model.RememberMe, 
                    lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl ?? "/");
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Validate email against whitelist
                string role = model.Role == "Faculty" ? "Faculty" : "Student";
                
                if (role == "Student" && !AllowedStudentEmails.Contains(model.Email ?? ""))
                {
                    ModelState.AddModelError(string.Empty, "❌ Access Denied: Your email is not authorized to register as a student.");
                    return View(model);
                }

                if (role == "Faculty" && model.Email != AllowedFacultyEmail)
                {
                    ModelState.AddModelError(string.Empty, "❌ Access Denied: Your email is not authorized to register as faculty.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password ?? "");
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Assign role
                    await _userManager.AddToRoleAsync(user, role);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User signed in.");

                    return LocalRedirect(returnUrl ?? "/");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
