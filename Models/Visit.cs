using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Models
{
    public class Visit
    {
        public int Id { get; set; }

        // FK to Employee (nullable - allows walk-in patients)
        public int? EmployeeId { get; set; }

        [Required, StringLength(100)]
        public string PatientName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Department { get; set; } = string.Empty;

        public int Age { get; set; }

        [StringLength(10)]
        public string Sex { get; set; } = string.Empty;

        [StringLength(15)]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [StringLength(80)]
        public string Occupation { get; set; } = string.Empty;

        public DateTime? Birthday { get; set; }

        [StringLength(300)]
        public string FamilyHistory { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string ChiefComplain { get; set; } = string.Empty;

        public DateTime DateOfConsultation { get; set; } = DateTime.Today;

        // Vital Signs stored as individual columns
        [StringLength(15)]  public string? VitalBP   { get; set; }
        [StringLength(10)]  public string? VitalPR   { get; set; }
        [StringLength(10)]  public string? VitalRR   { get; set; }
        [StringLength(10)]  public string? VitalTemp { get; set; }
        [StringLength(10)]  public string? VitalWT   { get; set; }
        [StringLength(10)]  public string? VitalHT   { get; set; }
        [StringLength(10)]  public string? VitalWC   { get; set; }

        // Social History
        public bool IsSmoker { get; set; }
        public int? SticksPerDay { get; set; }
        public bool IsAlcoholDrinker { get; set; }
        public int? BottlesAlcohol { get; set; }
        public bool IsBeverageDrinker { get; set; }
        public int? BottlesBeverage { get; set; }

        // Queue / Outcome
        [StringLength(15)]
        public string QueueStatus { get; set; } = "Waiting"; // Waiting | WithDoctor | Cleared

        [StringLength(200)]
        public string Outcome { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
