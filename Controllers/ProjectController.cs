using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CyberProjekt.Data;
using CyberProjekt.Models;
using CyberProjekt.Models.ViewModels;
using System.Security.Claims;

namespace CyberProjekt.Controllers
{
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string sortOrder, string search)
        {
            ViewBag.Search = search;
            ViewBag.CurrentSort = sortOrder;

            ViewBag.NameSort = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.StartDateSort = sortOrder == "dateStart" ? "dateStart_desc" : "dateStart";
            ViewBag.EndDateSort = sortOrder == "dateEnd" ? "dateEnd_desc" : "dateEnd";

            var projectsList = await _context.Projects.ToListAsync();
            var projects = projectsList.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLower();
                projects = projects.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
                    p.StartDate.ToString("yyyy-MM-dd").Contains(searchLower) ||
                    p.EndDate.ToString("yyyy-MM-dd").Contains(searchLower)
                );
            }

            switch (sortOrder)
            {
                case "name_desc": projects = projects.OrderByDescending(p => p.Name); break;
                case "dateStart": projects = projects.OrderBy(p => p.StartDate); break;
                case "dateStart_desc": projects = projects.OrderByDescending(p => p.StartDate); break;
                case "dateEnd": projects = projects.OrderBy(p => p.EndDate); break;
                case "dateEnd_desc": projects = projects.OrderByDescending(p => p.EndDate); break;
                default: projects = projects.OrderBy(p => p.Name); break;
            }

            ViewBag.ShowManagementActions = false;
            return View(projects.ToList());
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        [HttpGet]
        public IActionResult Create(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("MyProjects", "Project");
            return View();
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            if (ModelState.IsValid)
            {
                project.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        public async Task<IActionResult> MyProjects(string sortOrder, string search)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewBag.Search = search;
            ViewBag.CurrentSort = sortOrder;

            ViewBag.NameSort = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.StartDateSort = sortOrder == "dateStart" ? "dateStart_desc" : "dateStart";
            ViewBag.EndDateSort = sortOrder == "dateEnd" ? "dateEnd_desc" : "dateEnd";

            var myProjectsList = await _context.Projects
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var myProjects = myProjectsList.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLower();
                myProjects = myProjects.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
                    p.StartDate.ToString("yyyy-MM-dd").Contains(searchLower) ||
                    p.EndDate.ToString("yyyy-MM-dd").Contains(searchLower)
                );
            }

            switch (sortOrder)
            {
                case "name_desc": myProjects = myProjects.OrderByDescending(p => p.Name); break;
                case "dateStart": myProjects = myProjects.OrderBy(p => p.StartDate); break;
                case "dateStart_desc": myProjects = myProjects.OrderByDescending(p => p.StartDate); break;
                case "dateEnd": myProjects = myProjects.OrderBy(p => p.EndDate); break;
                case "dateEnd_desc": myProjects = myProjects.OrderByDescending(p => p.EndDate); break;
                default: myProjects = myProjects.OrderBy(p => p.Name); break;
            }

            ViewBag.ShowManagementActions = true;
            return View(myProjects.ToList());
        }

        [Authorize]
        public async Task<IActionResult> Details(int? id, string? returnUrl = null)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Comments)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedUser)
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index", "Project");
            return View(project);
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") && project.UserId != userId)
            {
                return Forbid();
            }

            return View(project);
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.ProjectId) return NotFound();

            var existingProject = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (existingProject == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") && existingProject.UserId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    project.UserId = existingProject.UserId;
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Projects.Any(p => p.ProjectId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(MyProjects));
            }

            return View(project);
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") && project.UserId != userId)
            {
                return Forbid();
            }

            return View(project);
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") && project.UserId != userId)
            {
                return Forbid();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyProjects));
        }

        [Authorize(Roles = "Administrator,Pracownik")]
        [HttpGet]
        public async Task<IActionResult> GetEditForm(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .Include(t => t.Creator)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .Include(t => t.Histories)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null) return NotFound();

            var users = await _context.Users
                .Select(u => new
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName
                })
                .ToListAsync();

            ViewBag.AssignedUserId = new SelectList(users, "Id", "FullName", task.AssignedUserId);

            var vm = new TaskDetailsViewModel
            {
                Task = task,
                Comments = task.Comments.ToList(),
                History = task.Histories.ToList()
            };

            ViewBag.ReturnUrl = Url.Action("Details", "Project", new { id = task.ProjectId });

            return PartialView("~/Views/ProjectTask/_EditTaskForm.cshtml", vm);
        }
    }
}