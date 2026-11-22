using System.Security.Claims;
using CyberProjekt.Data;
using CyberProjekt.Models;
using CyberProjekt.Models.ViewModels;
using CyberProjekt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberProjekt.Controllers
{
    public class AccountController : Controller
    {
        private const string GenericLoginError = "Login lub hasło niepoprawne";

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly AppDbContext _context;
        private readonly PasswordSecurityService _passwordService;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            AppDbContext context,
            PasswordSecurityService passwordService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _passwordService = passwordService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || user.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, GenericLoginError);
                return View(model);
            }

            bool loggedInWithOneTimeCode = false;
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!passwordValid)
            {
                if (_passwordService.VerifyOneTimeCode(user, model.Password))
                {
                    loggedInWithOneTimeCode = true;
                    user.OneTimeCodeUsed = true;
                    user.MustChangePassword = true;
                    await _userManager.UpdateAsync(user);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, GenericLoginError);
                    return View(model);
                }
            }

            await RefreshUserClaimsAndSignInAsync(user);

            var policy = await GetSecurityPolicyAsync();
            bool passwordExpired = (DateTime.UtcNow - user.PasswordChangedAt).TotalDays >= policy.PasswordMaxAgeDays;

            if (loggedInWithOneTimeCode || user.MustChangePassword || passwordExpired)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            return RedirectToAction("Dashboard", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var policy = await GetSecurityPolicyAsync();
            ViewBag.Policy = policy;

            return View(new ChangePasswordViewModel
            {
                IsForcedChange = user.MustChangePassword
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));

            var policy = await GetSecurityPolicyAsync();
            ViewBag.Policy = policy;
            model.IsForcedChange = user.MustChangePassword;

            if (!ModelState.IsValid)
                return View(model);

            if (!user.MustChangePassword)
            {
                if (string.IsNullOrEmpty(model.CurrentPassword) ||
                    !await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Aktualne hasło jest niepoprawne.");
                    return View(model);
                }
            }

            var validation = _passwordService.ValidatePassword(model.NewPassword, policy);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    ModelState.AddModelError(nameof(model.NewPassword), error);
                }
                return View(model);
            }

            var histories = await _context.PasswordHistories
                .Where(h => h.UserId == user.Id)
                .ToListAsync();

            if (_passwordService.IsPasswordInHistory(user, model.NewPassword, histories))
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Nowe hasło musi różnić się od poprzednio używanych haseł.");
                return View(model);
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var changeResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (!changeResult.Succeeded)
            {
                foreach (var error in changeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            user.MustChangePassword = false;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.OneTimeCodeHash = null;
            user.OneTimeCodeExpiresAt = null;
            user.OneTimeCodeUsed = true;
            await _userManager.UpdateAsync(user);

            _context.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = user.PasswordHash ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Hasło zostało pomyślnie zmienione.";
            return RedirectToAction("Dashboard", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(bool expired = false)
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");
            await _signInManager.SignOutAsync();

            if (expired)
            {
                return RedirectToAction("Index", "Home", new { expired = 1 });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        private async Task<SecurityPolicy> GetSecurityPolicyAsync()
        {
            return await _context.SecurityPolicies.FirstOrDefaultAsync()
                   ?? new SecurityPolicy();
        }

        private async Task RefreshUserClaimsAndSignInAsync(User user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var firstNameClaim = claims.FirstOrDefault(c => c.Type == "FirstName");
            var lastNameClaim = claims.FirstOrDefault(c => c.Type == "LastName");

            if (firstNameClaim != null) await _userManager.RemoveClaimAsync(user, firstNameClaim);
            if (lastNameClaim != null) await _userManager.RemoveClaimAsync(user, lastNameClaim);

            await _userManager.AddClaimsAsync(user, new[]
            {
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            });

            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        [HttpGet]
        public async Task<IActionResult> ExpiredLogout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { expired = 1 });
        }
    }
}