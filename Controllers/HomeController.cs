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
            var today = DateTime.Today;

            if (User.IsInRole("Goœæ"))
            {
                var allProjects = await _context.Projects
                    .Include(p => p.Tasks)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var allTasks = allProjects
                    .SelectMany(p => p.Tasks ?? Enumerable.Empty<ProjectTask>())
                    .Where(t => t.Status != Models.TaskStatus.Removed)
                    .ToList();

                var guestModel = new DashboardViewModel
                {
                    AllTasksCount = allTasks.Count,
                    InProgressTasksCount = allTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                    CompletedTasksCount = allTasks.Count(t => t.Status == Models.TaskStatus.Done),
                    OverdueTasksCount = allTasks.Count(t => t.Status != Models.TaskStatus.Done && t.Deadline < today),

                    ProjectNames = allProjects.Select(p => p.Name).ToList(),
                    NewTasks = allProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.New)).ToList(),
                    InProgressTasks = allProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.InProgress)).ToList(),
                    InVerificationTasks = allProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.InVerification)).ToList(),
                    DoneTasks = allProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.Done)).ToList(),

                    ProjectsProgress = allProjects.Select(p =>
                    {
                        var validTasks = (p.Tasks ?? Enumerable.Empty<ProjectTask>())
                            .Where(t => t.Status != Models.TaskStatus.Removed)
                            .ToList();

                        return new ProjectProgressViewModel
                        {
                            Name = p.Name,
                            Progress = validTasks.Any()
                                ? (int)((double)validTasks.Count(t => t.Status == Models.TaskStatus.Done) / validTasks.Count * 100)
                                : 0
                        };
                    }).ToList(),

                    UrgentTasks = new List<ProjectTask>()
                };

                ViewBag.AllProjectsForGuest = allProjects;
                return View(guestModel);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var myProjects = await _context.Projects
                .Include(p => p.Tasks.Where(t => t.AssignedUserId == userId))
                .Where(p => p.Tasks.Any(t => t.AssignedUserId == userId))
                .ToListAsync();

            var tasks = await _context.Tasks
                .Where(t => t.AssignedUserId == userId)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                AllTasksCount = tasks.Count,
                InProgressTasksCount = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                CompletedTasksCount = tasks.Count(t => t.Status == Models.TaskStatus.Done),
                OverdueTasksCount = tasks.Count(t => t.Status != Models.TaskStatus.Done && t.Deadline < today),

                ProjectNames = myProjects.Select(p => p.Name).ToList(),
                NewTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.New)).ToList(),
                InProgressTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.InProgress)).ToList(),
                InVerificationTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.InVerification)).ToList(),
                DoneTasks = myProjects.Select(p => p.Tasks.Count(t => t.Status == Models.TaskStatus.Done)).ToList()
            };

            model.ProjectsProgress = myProjects.Select(p => new ProjectProgressViewModel
            {
                Name = p.Name,
                Progress = p.Tasks.Any()
                    ? (int)((double)p.Tasks.Count(t => t.Status == Models.TaskStatus.Done) / p.Tasks.Count * 100)
                    : 0
            }).ToList();

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
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction("Index", "Home");
        }
    }
}