using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Models
{
    public class WellnessProgramRegistration
    {
        public int Id { get; set; }

        public int WellnessProgramId { get; set; }

        public int EmployeeId { get; set; }

        [Required, StringLength(100)]
        public string EmployeeName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Department { get; set; } = string.Empty;

        [StringLength(15)]
        public string Status { get; set; } = "Registered"; // Registered | Attended | Absent | Cancelled

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public DateTime? AttendedAt { get; set; }

        [StringLength(250)]
        public string Notes { get; set; } = string.Empty;

        // Navigation
        [ForeignKey("WellnessProgramId")]
        public WellnessProgram? WellnessProgram { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
    }
}
