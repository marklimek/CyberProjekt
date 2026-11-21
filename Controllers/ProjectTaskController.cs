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
    public class ProjectTaskController : Controller
    {

        private readonly AppDbContext _context;

        public ProjectTaskController(AppDbContext context)
        {
            _context = context;
        }
        private string GetPolishStatus(Models.TaskStatus status)
        {
            return status switch
            {
                Models.TaskStatus.New => "Nowe",
                Models.TaskStatus.InProgress => "W trakcie",
                Models.TaskStatus.InVerification => "W weryfikacji",
                Models.TaskStatus.Done => "Zrobione",
                Models.TaskStatus.Removed => "Usunięte",
                _ => status.ToString()
            };
        }
        public async Task<IActionResult> Index(string sortOrder, string search)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.Search = search;

            ViewBag.TitleSort = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewBag.ProjectSort = String.IsNullOrEmpty(sortOrder) ? "project_desc" : "";
            ViewBag.DateSort = sortOrder == "date" ? "date_desc" : "date";

            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .ToListAsync();

            if (!String.IsNullOrEmpty(search))
            {
                var searchTerms = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    tasks = tasks.Where(t =>
                        t.Title.ToLower().Contains(term) ||
                        (t.Project != null && t.Project.Name.ToLower().Contains(term)) ||
                        (t.AssignedUser != null && (t.AssignedUser.FirstName.ToLower().Contains(term) ||
                                                    t.AssignedUser.LastName.ToLower().Contains(term))) ||
                        GetPolishStatus(t.Status).ToLower().Contains(term) ||
                        t.Status.ToString().ToLower().Contains(term) ||
                        t.Deadline.ToString("yyyy-MM-dd").Contains(term)
                    ).ToList();
                }
            }
            tasks = sortOrder switch
            {
                "project" => tasks.OrderBy(t => t.Project?.Name?.Trim() ?? "").ToList(),
                "project_desc" => tasks.OrderByDescending(t => t.Project?.Name?.Trim() ?? "").ToList(),

                "title_desc" => tasks.OrderByDescending(t => t.Title?.Trim() ?? "").ToList(),

                "date" => tasks.OrderBy(t => t.Deadline).ToList(),
                "date_desc" => tasks.OrderByDescending(t => t.Deadline).ToList(),
                _ => tasks.OrderBy(t => t.Title?.Trim() ?? "").ToList()
            };

            return View(tasks);
        }


        [Authorize]
        public async Task<IActionResult> MyTasks(string sortOrder, string search)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.Search = search;

            ViewBag.TitleSort = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewBag.ProjectSort = String.IsNullOrEmpty(sortOrder) ? "project_desc" : "";
            ViewBag.DateSort = sortOrder == "date" ? "date_desc" : "date";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myTasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .Where(p => p.AssignedUserId == userId)
                .ToListAsync();
            if (!String.IsNullOrEmpty(search))
            {
                var searchTerms = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    myTasks = myTasks.Where(t =>
                        t.Title.ToLower().Contains(term) ||
                        (t.Project != null && t.Project.Name.ToLower().Contains(term)) ||
                        (t.AssignedUser != null && (t.AssignedUser.FirstName.ToLower().Contains(term) ||
                                                    t.AssignedUser.LastName.ToLower().Contains(term))) ||
                        t.Priority.ToString().ToLower().Contains(term) ||
                        GetPolishStatus(t.Status).ToLower().Contains(term) ||
                        t.Status.ToString().ToLower().Contains(term) ||
                        t.Deadline.ToString("yyyy-MM-dd").Contains(term)
                    ).ToList();
                }
            }
            myTasks = sortOrder switch
            {
                "project" => myTasks.OrderBy(t => t.Project?.Name?.Trim() ?? "").ToList(),
                "project_desc" => myTasks.OrderByDescending(t => t.Project?.Name?.Trim() ?? "").ToList(),

                "title_desc" => myTasks.OrderByDescending(t => t.Title?.Trim() ?? "").ToList(),

                "date" => myTasks.OrderBy(t => t.Deadline).ToList(),
                "date_desc" => myTasks.OrderByDescending(t => t.Deadline).ToList(),
                _ => myTasks.OrderBy(t => t.Title?.Trim() ?? "").ToList()
            };

            return View(myTasks);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create(int? projectId)
        {
            ViewBag.ProjectId = new SelectList(_context.Projects, "ProjectId", "Name", projectId);
            var users = _context.Users.Select(u => new {
                Id = u.Id,
                DisplayName = $"{u.FirstName} {u.LastName} - {u.UserName}"
            }).ToList();

            ViewBag.AssignedUserId = new SelectList(users, "Id", "DisplayName");

            ViewBag.SelectedProjectId = projectId;
            if (projectId.HasValue)
            {
                var project = _context.Projects
                    .FirstOrDefault(p => p.ProjectId == projectId);

                ViewBag.ProjectName = project?.Name;
            }
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectTask task)
        {
            ModelState.Remove("Project");
            ModelState.Remove("AssignedUser");

            if (ModelState.IsValid)
            {
                task.CreatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                task.Created = DateTime.Now;
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var users = _context.Users.Select(u => new
            {
                Id = u.Id,
                DisplayName = $"{u.FirstName} {u.LastName} - {u.UserName}"
            }).ToList();

            ViewBag.ProjectId = new SelectList(_context.Projects, "ProjectId", "Name", task.ProjectId);
            ViewBag.AssignedUserId = new SelectList(users, "Id", "DisplayName", task.AssignedUserId);
            ViewBag.SelectedProjectId = task.ProjectId;

            return View(task);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .Include(t => t.Creator)
                .Include(t => t.Histories)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Attachments)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.TaskId == id);

            if (task == null) return NotFound();
            if (task.IsDeleted)
            {
                return View("TaskRemoved");
            }

            var users = await _context.Users
                .Select(u => new { Id = u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToListAsync();

            ViewBag.AssignedUserId = new SelectList(users, "Id", "FullName", task.AssignedUserId);

            var activities = new List<ActivityViewModel>();
            // Dodaj zmiany statusu
            activities.AddRange(task.Histories.Select(h => new ActivityViewModel
            {
                Date = h.ChangeDate,
                Author = h.ChangedBy,
                Description = h.ActionDescription,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                IsComment = false
            }));

            // Dodaj komentarze
            activities.AddRange(task.Comments.Select(c => new ActivityViewModel
            {
                Date = c.CreatedAt,
                Author = $"{c.User?.FirstName} {c.User?.LastName}",
                Description = c.Content,
                IsComment = true
            }));

            var viewModel = new TaskDetailsViewModel
            {
                Task = task,
                Comments = task.Comments.OrderByDescending(c => c.CreatedAt).ToList(),
                History = task.Histories.OrderByDescending(h => h.ChangeDate).ToList(),
                Activities = activities.OrderByDescending(a => a.Date).ToList()
            };
            ViewBag.UsersDictionary = await _context.Users
                .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");
            ViewBag.ReturnUrl = Url.Action(
                "Details",
                "ProjectTask",
                new { id = task.TaskId 
            });

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, Models.TaskStatus status)
        {
            var task = await _context.Tasks.Include(t => t.Histories).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null) return NotFound();

            var history = new TaskHistory
            {
                TaskId = id,
                ChangedBy = $"{User.FindFirstValue("FirstName")} {User.FindFirstValue("LastName")}" ?? "System",
                ActionDescription = "Zmiana statusu",
                OldValue = task.Status.ToString(),
                NewValue = status.ToString(),
                ChangeDate = DateTime.Now
            };

            task.Status = status;
            task.Updated = DateTime.Now;
            task.Histories.Add(history);

            _context.Update(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InlineEdit(int id, string title, string description, Models.TaskStatus? status, string assignedUserId, DateTime? deadline, Models.TaskPriority? priority)
        {
            var task = await _context.Tasks.Include(t => t.Histories).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isAuthor = task.CreatorId == currentUserId; // dopasuj do swojej nazwy pola autora w bazie
            var isAssigned = task.AssignedUserId == currentUserId;

            var changerName = $"{User.FindFirstValue("FirstName")} {User.FindFirstValue("LastName")}" ?? "Użytkownik";

            if (!isAdmin && !isAuthor && !isAssigned)
            {
                return Forbid();
            }

            bool isUpdated = false;

            void AddHistoryLog(string action, string oldVal, string newVal)
            {
                task.Histories.Add(new TaskHistory
                {
                    TaskId = id,
                    ChangedBy = changerName,
                    ActionDescription = action,
                    OldValue = oldVal,
                    NewValue = newVal,
                    ChangeDate = DateTime.Now
                });
                isUpdated = true;
            }


            if (isAdmin || isAuthor)
            {
                if (title != null && task.Title != title) { AddHistoryLog("Zmiana tytułu", task.Title, title); task.Title = title; }
                if (description != null && task.Description != description) { AddHistoryLog("Zmiana opisu", task.Description, description); task.Description = description; }
                if (priority.HasValue && task.Priority != priority.Value) { AddHistoryLog("Zmiana priorytetu", task.Priority.ToString(), priority.Value.ToString()); task.Priority = priority.Value; }
            }

            if (isAdmin || isAuthor || isAssigned)
            {
                if (status.HasValue && task.Status != status.Value) { AddHistoryLog("Zmiana statusu", task.Status.ToString(), status.Value.ToString()); task.Status = status.Value; }
                if (assignedUserId != task.AssignedUserId) { AddHistoryLog("Zmiana przypisania", task.AssignedUserId ?? "Brak", assignedUserId ?? "Brak"); task.AssignedUserId = assignedUserId; }
                if (deadline.HasValue && task.Deadline.Date != deadline.Value.Date) { AddHistoryLog("Zmiana terminu", task.Deadline.ToString("yyyy-MM-dd"), deadline.Value.ToString("yyyy-MM-dd")); task.Deadline = deadline.Value; }
            }

            if (isUpdated)
            {
                task.Updated = DateTime.Now;
                _context.Update(task);
                await _context.SaveChangesAsync();
            }

            string returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null) return NotFound();

            // Sprawdzenie uprawnień (Admin, Autor lub Przypisany)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var isAuthor = task.CreatorId == currentUserId;
            var isAssigned = task.AssignedUserId == currentUserId;

            if (!isAdmin && !isAuthor && !isAssigned) return Forbid();

            // Logika miękkiego usuwania
            task.Status = Models.TaskStatus.Removed;
            task.IsDeleted = true;
            task.Updated = DateTime.Now;

            _context.Update(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
