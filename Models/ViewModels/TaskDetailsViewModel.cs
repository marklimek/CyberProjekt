namespace CyberProjekt.Models.ViewModels
{
    public class TaskDetailsViewModel
    {
        public ProjectTask Task { get; set; }
        public List<Comment> Comments { get; set; }
        public List<TaskHistory> History { get; set; }
        public string NewCommentText { get; set; }
        public IFormFile? NewAttachment { get; set; }
        public List<ActivityViewModel> Activities { get; set; } = new List<ActivityViewModel>();
    }
}
