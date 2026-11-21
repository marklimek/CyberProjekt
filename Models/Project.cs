using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? UserId { get; set; }
        public User? CreatedBy { get; set; }
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}
