using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberProjekt.Data;
using CyberProjekt.Models;
using CyberProjekt.Models.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace CyberProjekt.Controllers
{
    public class HomeController : Controller
    {

        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        [Route("Dashboard")]
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var myProjects = await _context.Projects
                .Include(p => p.Tasks.Where(t => t.AssignedUserId == userId))
                .Where(p => p.Tasks.Any(t => t.AssignedUserId == userId))
                .ToListAsync();

            var tasks = await _context.Tasks
                .Where(t => t.AssignedUserId == userId)
                .ToListAsync();

            //var projects = await _context.Projects
            //    .Include(p => p.Tasks.Where(t => t.AssignedUserId == userId))
            //    .ToListAsync();

            var model = new DashboardViewModel
            {
                AllTasksCount = tasks.Count,
                InProgressTasksCount = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                CompletedTasksCount = tasks.Count(t => t.Status == Models.TaskStatus.Done),
                OverdueTasksCount = tasks.Count(t => t.Status != Models.TaskStatus.Done && t.Deadline < today),

                // Dane do wykresu - tylko dla "moich" projektów
                ProjectNames = myProjects.Select(p => p.Name).ToList(),
                NewTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.New)).ToList(),
                InProgressTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.InProgress)).ToList(),
                InVerificationTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.InVerification)).ToList(),
                DoneTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.Done)).ToList()
            };

            // W Dashboard w HomeController.cs
            model.ProjectsProgress = myProjects.Select(p => new ProjectProgressViewModel
            {
                Name = p.Name,
                // Procent ukoñczenia zadañ u¿ytkownika w danym projekcie
                Progress = p.Tasks.Any()
            ? (int)((double)p.Tasks.Count(t => t.Status == Models.TaskStatus.Done) / p.Tasks.Count * 100)
            : 0
            }).ToList();

            // 4. Najpilniejsze zadania
            model.UrgentTasks = await _context.Tasks
                .Include(t => t.Project)
                .Where(t => t.AssignedUserId == userId && t.Status != Models.TaskStatus.Done)
                .OrderBy(t => t.Deadline)
                .Take(5)
                .ToListAsync();

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult RedirectHome()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
