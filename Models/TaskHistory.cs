namespace CyberProjekt.Models
{
    public class TaskHistory
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public ProjectTask Task { get; set; }

        public string ChangedBy { get; set; } 
        public DateTime ChangeDate { get; set; } = DateTime.Now;
        public string ActionDescription { get; set; } 
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}