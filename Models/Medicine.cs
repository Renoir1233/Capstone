using System.ComponentModel.DataAnnotations;

namespace Capstone.Models
{
    public class Medicine
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        public int CurrentStock { get; set; }

        public int MinStock { get; set; } = 20;   // threshold for "Low"
        public int CriticalStock { get; set; } = 10; // threshold for "Critical"
        public int MaxStock { get; set; } = 100;

        [StringLength(20)]
        public string Unit { get; set; } = "tablets";

        [StringLength(30)]
        public string BatchNumber { get; set; } = string.Empty;

        public DateTime? ExpiryDate { get; set; }

        public DateTime? LastRestockedAt { get; set; }
        public DateTime? LastDispensedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Computed status (not stored)
        public string StockStatus =>
            CurrentStock <= CriticalStock ? "Critical" :
            CurrentStock <= MinStock      ? "Low"      : "Good";

        public int StockPercent =>
            MaxStock == 0 ? 0 : (int)Math.Round((double)CurrentStock / MaxStock * 100);

        // Navigation
        public ICollection<MedicineTransaction> Transactions { get; set; } = new List<MedicineTransaction>();
    }
}
