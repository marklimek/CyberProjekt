using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CyberProjekt.Data;
using CyberProjekt.Models;
using CyberProjekt.Models.ViewModels;
using System.Security.Claims;

namespace CyberProjekt.Controllers
{
    public class CommentController : Controller
    {
        private readonly AppDbContext _context;
        public CommentController(AppDbContext context) => _context = context;

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TaskDetailsViewModel model, string? returnUrl)
        {
            if (model.Task == null || model.Task.TaskId == 0) return BadRequest();

            var comment = new Comment
            {
                TaskId = model.Task.TaskId,
                Content = model.NewCommentText,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Details", "ProjectTask", new {id = model.Task.TaskId});
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int commentId, string returnUrl)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (comment.UserId != currentUserId) return Forbid(); // Tylko autor
            int taskId = comment.TaskId; // zapamiętaj ID zadania13

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Details", "ProjectTask", new {id = taskId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int commentId, string newContent, string returnUrl)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (comment.UserId != currentUserId) return Forbid(); // Tylko autor
            comment.Content = newContent;
            comment.UpdatedAt = DateTime.Now;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            return LocalRedirect(returnUrl);
        }
    }
}
