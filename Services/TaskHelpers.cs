using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Services
{
    public static class TaskHelpers
    {
        public static string GetStatusColor(Models.TaskStatus status) => status switch
        {
            Models.TaskStatus.New => "#6366f1",
            Models.TaskStatus.InProgress => "#eab308",
            Models.TaskStatus.InVerification => "#f97316",
            Models.TaskStatus.Done => "#22c55e",
            _ => "#94a3b8"
        };

        public static string GetDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DisplayAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayAttribute));
            return attribute != null ? attribute.Name : value.ToString();
        }
    }
}