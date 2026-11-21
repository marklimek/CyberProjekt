using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models
{
    public enum TaskStatus {
        [Display(Name = "Nowe")] New,
        [Display(Name = "W trakcie")] InProgress,
        [Display(Name = "W weryfikacji")] InVerification,
        [Display(Name = "Zakończone")] Done,
        [Display(Name = "Usunięte")] Removed
    }
    public enum TaskPriority { Niski, Średni, Wysoki, Krytyczny }
    public class ProjectTask
    {
        [Key]
        public int TaskId { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Updated { get; set; }
        public DateTime Deadline { get; set; }
        public TaskStatus Status { get; set; }
        public int ProjectId { get; set; }
        public Project? Project { get; set; }
        public string? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }
        public string? CreatorId { get; set; }
        public User? Creator { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<TaskHistory> Histories { get; set; } = new List<TaskHistory>();

    }
}
