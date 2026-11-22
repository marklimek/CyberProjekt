using System.Security.Claims;
using CyberProjekt.Data;
using CyberProjekt.Models;
using CyberProjekt.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberProjekt.Controllers
{
    [Authorize(Roles = "Administrator,Pracownik")]
    public class CommentController : Controller
    {
        private readonly AppDbContext _context;
        public CommentController(AppDbContext context) => _context = context;

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
            return RedirectToAction("Details", "ProjectTask", new { id = model.Task.TaskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int commentId, string? returnUrl)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") && comment.UserId != currentUserId) return Forbid();
            int taskId = comment.TaskId;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Details", "ProjectTask", new { id = taskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int commentId, string newContent, string? returnUrl)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") && comment.UserId != currentUserId) return Forbid();
            comment.Content = newContent;
            comment.UpdatedAt = DateTime.Now;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Details", "ProjectTask", new { id = comment.TaskId });
        }
    }
}