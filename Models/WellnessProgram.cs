using System.ComponentModel.DataAnnotations;

namespace Capstone.Models
{
    public class WellnessProgram
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [StringLength(100)]
        public string Venue { get; set; } = string.Empty;

        [Required]
        public DateTime ProgramDate { get; set; }

        [StringLength(50)]
        public string TargetDepartment { get; set; } = "All Departments";

        public int ParticipantCapacity { get; set; } = 50;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Scheduled"; // Scheduled | Open | Ongoing | Completed | Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? CompletedAt { get; set; }

        // Navigation
        public ICollection<WellnessProgramRegistration> Registrations { get; set; } = new List<WellnessProgramRegistration>();
    }
}
