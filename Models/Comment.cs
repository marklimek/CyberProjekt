using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public int TaskId { get; set; }
        public ProjectTask Task { get; set; }
        public string? UserId { get; set; }
        public User? User { get; set; }

        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
