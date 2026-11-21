namespace CyberProjekt.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int AllTasksCount { get; set; }
        public int InProgressTasksCount { get; set; }
        public int OverdueTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public List<string> ProjectNames { get; set; }
        public List<int> NewTasks { get; set; }
        public List<int> InProgressTasks { get; set; }
        public List<int> InVerificationTasks { get; set; }
        public List<int> DoneTasks { get; set; }
        public List<ProjectProgressViewModel> ProjectsProgress { get; set; }
        public List<CyberProjekt.Models.ProjectTask> UrgentTasks { get; set; }
    }
    

public class ProjectProgressViewModel
    {
        public string Name { get; set; }
        public int Progress { get; set; }
    }
}