using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Models
{
    public class MedicineTransaction
    {
        public int Id { get; set; }

        public int MedicineId { get; set; }

        [Required, StringLength(10)]
        public string TransactionType { get; set; } = string.Empty; // "Restock" | "Dispense"

        public int Quantity { get; set; }

        [StringLength(250)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(100)]
        public string PerformedBy { get; set; } = "Admin";

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("MedicineId")]
        public Medicine? Medicine { get; set; }
    }
}
