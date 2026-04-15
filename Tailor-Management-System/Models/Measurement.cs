using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tailor_Management_System.Models
{
    public class Measurement
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

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Type { get; set; } // Shirt, Pant, Suit

        // Storing complex objects as JSON strings
        public string? ShirtData { get; set; }
        public string? PantData { get; set; }

        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
