using System.Security.Claims;
using System.Security.Cryptography;
using CyberProjekt.Data;
using CyberProjekt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberProjekt.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AdminController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.AssignedTasks)
                .Where(u => u.Email != "unknown@system.local")
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var policy = await _context.SecurityPolicies.FirstOrDefaultAsync();
            if (policy == null)
            {
                policy = new SecurityPolicy
                {
                    MinPasswordLength = 12,
                    IsEntropyPolicyEnabled = true,
                    MinEntropyBits = 60.0,
                    PasswordMaxAgeDays = 90,
                    SessionIdleTimeoutMinutes = 15
                };
                _context.SecurityPolicies.Add(policy);
                await _context.SaveChangesAsync();
            }

            ViewBag.SecurityPolicy = policy;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string username, string firstName, string lastName, string email, string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Login oraz adres e-mail są wymagane.";
                return RedirectToAction(nameof(Index));
            }

            if (await _userManager.FindByNameAsync(username) != null)
            {
                TempData["Error"] = $"Użytkownik o loginie '{username}' już istnieje.";
                return RedirectToAction(nameof(Index));
            }

            var user = new User
            {
                UserName = username.Trim(),
                Email = email.Trim(),
                EmailConfirmed = true,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                MustChangePassword = true,
                IsBlocked = false,
                PasswordChangedAt = DateTime.UtcNow
            };

            var tempPassword = $"Tmp#{Guid.NewGuid():N}A1!";
            var result = await _userManager.CreateAsync(user, tempPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            await _userManager.AddClaimsAsync(user, new[]
            {
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            });

            if (await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            var oneTimeCode = SetOneTimeCode(user);
            await _userManager.UpdateAsync(user);

            TempData["GeneratedCode"] = oneTimeCode;
            TempData["GeneratedCodeUser"] = user.UserName;
            TempData["Success"] = $"Utworzono konto '{user.UserName}' z rolą {role}.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string email, string? role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Imię, nazwisko oraz adres e-mail nie mogą być puste.";
                return RedirectToAction(nameof(Index));
            }

            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();
            user.Email = email.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var oldFirst = claims.FirstOrDefault(c => c.Type == "FirstName");
            var oldLast = claims.FirstOrDefault(c => c.Type == "LastName");
            if (oldFirst != null) await _userManager.RemoveClaimAsync(user, oldFirst);
            if (oldLast != null) await _userManager.RemoveClaimAsync(user, oldLast);

            await _userManager.AddClaimsAsync(user, new[]
            {
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName)
            });

            if (!string.IsNullOrWhiteSpace(role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault();

                if (!string.Equals(currentRole, role, StringComparison.Ordinal))
                {
                    if (IsBuiltInAdmin(user) && role != "Administrator")
                    {
                        TempData["Error"] = "Zaktualizowano dane, ale nie można odebrać roli Administrator głównemu kontu ADMIN.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (currentRoles.Contains("Administrator") && role != "Administrator")
                    {
                        var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                        if (admins.Count <= 1)
                        {
                            TempData["Error"] = "Zaktualizowano dane, ale musi zostać przynajmniej jeden administrator w systemie.";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (await _roleManager.RoleExistsAsync(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            TempData["Success"] = $"Zapisano zmiany dla użytkownika '{user.UserName}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateOneTimeCode(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var oneTimeCode = SetOneTimeCode(user);
            await _userManager.UpdateAsync(user);

            TempData["GeneratedCode"] = oneTimeCode;
            TempData["GeneratedCodeUser"] = user.UserName;
            TempData["Success"] = $"Wygenerowano nowy 8-cyfrowy kod jednorazowy dla użytkownika '{user.UserName}'.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (IsBuiltInAdmin(user))
            {
                TempData["Error"] = "Nie można zablokować głównego konta ADMIN.";
                return RedirectToAction(nameof(Index));
            }

            user.IsBlocked = !user.IsBlocked;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsBlocked
                ? $"Konto '{user.UserName}' zostało zablokowane."
                : $"Konto '{user.UserName}' zostało odblokowane.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string newRole)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("Użytkownik nie istnieje.");

            if (IsBuiltInAdmin(user) && newRole != "Administrator")
            {
                TempData["Error"] = "Nie można odebrać roli Administrator głównemu kontu ADMIN.";
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("Administrator") && newRole != "Administrator")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "Musi zostać przynajmniej jeden administrator w systemie.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"Zmieniono rolę użytkownika '{user.UserName}' na {newRole}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (IsBuiltInAdmin(user))
            {
                TempData["Error"] = "Nie można usunąć głównego konta ADMIN.";
                return RedirectToAction(nameof(Index));
            }

            var admins = await _userManager.GetUsersInRoleAsync("Administrator");
            if (await _userManager.IsInRoleAsync(user, "Administrator") && admins.Count <= 1)
            {
                TempData["Error"] = "Nie można usunąć jedynego administratora w systemie.";
                return RedirectToAction(nameof(Index));
            }

            var unknownUser = await _userManager.FindByEmailAsync("unknown@system.local");
            if (unknownUser == null)
            {
                unknownUser = new User
                {
                    UserName = "unknown@system.local",
                    Email = "unknown@system.local",
                    FirstName = "Unknown",
                    LastName = "User"
                };

                await _userManager.CreateAsync(unknownUser);
                unknownUser = await _userManager.FindByEmailAsync("unknown@system.local");
            }

            var assignedTasks = await _context.Tasks.Where(t => t.AssignedUserId == id).ToListAsync();
            foreach (var task in assignedTasks) task.AssignedUserId = unknownUser!.Id;

            var creatorTasks = await _context.Tasks.Where(t => t.CreatorId == id).ToListAsync();
            foreach (var task in creatorTasks) task.CreatorId = unknownUser!.Id;

            var projects = await _context.Projects.Where(p => p.UserId == id).ToListAsync();
            foreach (var project in projects) project.UserId = unknownUser!.Id;

            var comments = await _context.Comments.Where(c => c.UserId == id).ToListAsync();
            foreach (var comment in comments) comment.UserId = unknownUser!.Id;

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Success"] = $"Użytkownik '{user.UserName}' został usunięty.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSecurityPolicy(
            int minPasswordLength,
            bool isEntropyPolicyEnabled,
            double minEntropyBits,
            int passwordMaxAgeDays,
            int sessionIdleTimeoutMinutes)
        {
            if (minPasswordLength < 4 || minPasswordLength > 128 ||
                minEntropyBits < 10 || minEntropyBits > 256 ||
                passwordMaxAgeDays < 0 || passwordMaxAgeDays > 3650 ||
                sessionIdleTimeoutMinutes < 1 || sessionIdleTimeoutMinutes > 1440)
            {
                TempData["Error"] = "Podano nieprawidłowe wartości parametrów polityki bezpieczeństwa.";
                return RedirectToAction(nameof(Index), new { tab = "policy" });
            }

            var policy = await _context.SecurityPolicies.FirstOrDefaultAsync();
            if (policy == null)
            {
                policy = new SecurityPolicy();
                _context.SecurityPolicies.Add(policy);
            }

            policy.MinPasswordLength = minPasswordLength;
            policy.IsEntropyPolicyEnabled = isEntropyPolicyEnabled;
            policy.MinEntropyBits = minEntropyBits;
            policy.PasswordMaxAgeDays = passwordMaxAgeDays;
            policy.SessionIdleTimeoutMinutes = sessionIdleTimeoutMinutes;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Polityka bezpieczeństwa i sesji została zaktualizowana.";
            return RedirectToAction(nameof(Index), new { tab = "policy" });
        }

        public static bool RequiresPasswordChange(User user, SecurityPolicy policy)
        {
            if (user.MustChangePassword)
                return true;

            if (policy.PasswordMaxAgeDays >= 0 &&
                (DateTime.UtcNow - user.PasswordChangedAt).TotalDays >= policy.PasswordMaxAgeDays)
                return true;

            return false;
        }

        private string SetOneTimeCode(User user)
        {
            var code = RandomNumberGenerator.GetInt32(10000000, 100000000).ToString();
            user.OneTimeCodeHash = _userManager.PasswordHasher.HashPassword(user, code);
            user.OneTimeCodeExpiresAt = DateTime.UtcNow.AddHours(1);
            user.OneTimeCodeUsed = false;
            user.MustChangePassword = true;
            return code;
        }

        private static bool IsBuiltInAdmin(User user) =>
            string.Equals(user.UserName, "ADMIN", StringComparison.OrdinalIgnoreCase);
    }
}