using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        // FK to Visit
        public int VisitId { get; set; }

        // FK to Medicine
        public int MedicineId { get; set; }

        [Required, StringLength(150)]
        public string MedicineName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [StringLength(50)]
        public string Dosage { get; set; } = string.Empty;

        [StringLength(50)]
        public string Frequency { get; set; } = string.Empty;

        [StringLength(30)]
        public string Duration { get; set; } = string.Empty;

        [StringLength(250)]
        public string Instructions { get; set; } = string.Empty;

        public DateTime PrescribedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string PrescribedBy { get; set; } = string.Empty;

        public bool IsDispensed { get; set; } = false;

        public DateTime? DispensedDate { get; set; }

        // Navigation
        [ForeignKey("VisitId")]
        public Visit? Visit { get; set; }

        [ForeignKey("MedicineId")]
        public Medicine? Medicine { get; set; }
    }
}
