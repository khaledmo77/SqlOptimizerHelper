using System.ComponentModel.DataAnnotations;

namespace SampleWebApi.Models
{
    /// <summary>
    /// Represents an order in the e-commerce system
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Unique identifier for the order
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the customer who placed the order
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Order number (unique identifier for customers)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Order status
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Total amount of the order
        /// </summary>
        [Required]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Date when the order was placed
        /// </summary>
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the order was shipped
        /// </summary>
        public DateTime? ShippedDate { get; set; }

        /// <summary>
        /// Date when the order was delivered
        /// </summary>
        public DateTime? DeliveredDate { get; set; }

        /// <summary>
        /// Shipping address
        /// </summary>
        [MaxLength(500)]
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// Notes for the order
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Navigation property to the customer who placed the order
        /// </summary>
        public virtual Customer Customer { get; set; } = null!;

        /// <summary>
        /// Navigation property for order items
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
