using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tailor_Management_System.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        public int? MeasurementId { get; set; }

        [ForeignKey("MeasurementId")]
        public Measurement? Measurement { get; set; }

        // Storing arrays as JSON
        public string? Services { get; set; } 
        public string? ItemsUsed { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed
        public string PaymentMethod { get; set; } = "Pending"; // Cash, Online, Pending
        
        public DateTime? PaymentExpectedBy { get; set; }

        public int? AssignedTailorId { get; set; }

        [ForeignKey("AssignedTailorId")]
        public Tailor? AssignedTailor { get; set; }

        public decimal TailorContractPrice { get; set; } = 0;
        public decimal CalculatedTotal { get; set; } = 0;
        public decimal UserDefinedTotal { get; set; } = 0;
        
        public DateTime? TargetDeliveryDate { get; set; }
        
        public decimal TotalAmount { get; set; } = 0; // Legacy / Fallback

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
