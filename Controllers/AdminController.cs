using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberProjekt.Data;
using CyberProjekt.Models;

namespace CyberProjekt.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
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
                 .ToListAsync();

            return View(users);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Nie pozwól usunąć ostatniego administratora
            var admins = await _userManager.GetUsersInRoleAsync("Administrator");

            if (await _userManager.IsInRoleAsync(user, "Administrator") &&
                admins.Count <= 1)
            {
                TempData["Error"] =
                    "Nie można usunąć jedynego administratora w systemie.";

                return RedirectToAction("Index");
            }

            // Znajdź lub utwórz użytkownika zastępczego
            var unknownUser =
                await _userManager.FindByEmailAsync("unknown@system.local");

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
                unknownUser =
                    await _userManager.FindByEmailAsync("unknown@system.local");
            }

            // Zadania przypisane użytkownikowi
            var assignedTasks = await _context.Tasks
                .Where(t => t.AssignedUserId == id)
                .ToListAsync();

            foreach (var task in assignedTasks)
            {
                task.AssignedUserId = unknownUser.Id;
            }

            // Zadania utworzone przez użytkownika
            var creatorTasks = await _context.Tasks
                .Where(t => t.CreatorId == id)
                .ToListAsync();

            foreach (var task in creatorTasks)
            {
                task.CreatorId = unknownUser.Id;
            }

            // Projekty utworzone przez użytkownika
            var projects = await _context.Projects
                .Where(p => p.UserId == id)
                .ToListAsync();

            foreach (var project in projects)
            {
                project.UserId = unknownUser.Id;
            }

            // Komentarze użytkownika
            var comments = await _context.Comments
                .Where(c => c.UserId == id)
                .ToListAsync();

            foreach (var comment in comments)
            {
                comment.UserId = unknownUser.Id;
            }

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(
                    ", ",
                    result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string newRole)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Użytkownik nie istnieje.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("Administrator") && newRole != "Administrator")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                if (admins.Count <= 1)
                {
                    TempData["Error"] = "Musi zostać przynajmniej jeden administrator w systemie.";
                    return RedirectToAction("Index");
                }
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string firstName, string lastName, string email, string password, string role)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddClaimsAsync(user, new[]
                {
            new System.Security.Claims.Claim("FirstName", user.FirstName),
            new System.Security.Claims.Claim("LastName", user.LastName)
        });

                await _userManager.AddToRoleAsync(user, role);

                return RedirectToAction("Index"); 
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return RedirectToAction("Index");
        }
    }
}