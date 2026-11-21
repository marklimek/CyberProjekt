using Microsoft.AspNetCore.Mvc;
using CyberProjekt.Data;
using CyberProjekt.Models;

namespace CyberProjekt.Controllers
{
    public class AttachmentController : Controller
    {
        private readonly AppDbContext _context;
        public AttachmentController(AppDbContext context) => _context = context;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToComment(int commentId, int taskId, IFormFile newAttachment)
        {
            if (newAttachment != null && newAttachment.Length > 0)
            {
                // 1. Zdefiniuj ścieżkę do folderu
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                // 2. SPRAWDŹ I UTWÓRZ FOLDER, jeśli nie istnieje
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 3. Połącz ścieżkę folderu z nazwą pliku
                var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(newAttachment.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                // 4. Teraz zapis pliku zadziała
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newAttachment.CopyToAsync(stream);
                }

                var attachment = new Attachment
                {
                    CommentId = commentId,
                    FileName = newAttachment.FileName,
                    FilePath = "/uploads/" + fileName // Zapisz ścieżkę relatywną dla przeglądarki
                };
                _context.Attachments.Add(attachment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "ProjectTask", new { id = taskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToTask(int taskId, IFormFile attachment)
        {
            if (attachment != null && attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + "_" + attachment.FileName;
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(stream);
                }

                _context.Attachments.Add(new Attachment
                {
                    ProjectTaskId = taskId,
                    FileName = attachment.FileName,
                    FilePath = "/uploads/" + fileName
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                "Details",
                "ProjectTask",
                new { id = taskId });
        }

    }
}
