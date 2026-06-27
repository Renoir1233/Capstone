using System.ComponentModel.DataAnnotations;

namespace Capstone.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Department { get; set; } = string.Empty;

        public int Age { get; set; }

        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [StringLength(80)]
        public string Occupation { get; set; } = string.Empty;

        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [StringLength(15)]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime? Birthday { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
    }
}
