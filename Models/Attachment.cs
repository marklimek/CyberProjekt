using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models
{
    public class Attachment
    {
        [Key]
        public int AttachmentId { get; set; }
        [Required]
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public int? ProjectTaskId { get; set; }
        public ProjectTask? ProjectTask { get; set; }

        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }
    }
}
