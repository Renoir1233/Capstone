using System.ComponentModel.DataAnnotations;

namespace Capstone.Models
{
    public class HealthReminder
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required, StringLength(100)]
        public string ReminderType { get; set; } = string.Empty; // Annual Physical Exam, Flu Vaccination, etc.

        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        [StringLength(250)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Employee? Employee { get; set; }
    }
}
