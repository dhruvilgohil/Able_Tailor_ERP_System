using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tailor_Management_System.Models
{
    public class Tailor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? Phone { get; set; }

        [Required]
        public string PaymentType { get; set; } = "Salary"; // Salary, Contract

        public decimal Salary { get; set; } = 0;
        public decimal ContractRate { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
