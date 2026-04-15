using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tailor_Management_System.Models
{
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public decimal StockQty { get; set; } = 0;

        [Required]
        public string StockUnit { get; set; } = string.Empty;

        [Required]
        public decimal UnitPrice { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
