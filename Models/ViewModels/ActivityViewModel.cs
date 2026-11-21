namespace CyberProjekt.Models.ViewModels
{
    public class ActivityViewModel
    {
        public DateTime Date { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool IsComment { get; set; }
    }
}
