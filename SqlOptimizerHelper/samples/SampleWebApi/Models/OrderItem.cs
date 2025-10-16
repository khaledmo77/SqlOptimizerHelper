using System.ComponentModel.DataAnnotations;

namespace SampleWebApi.Models
{
    /// <summary>
    /// Represents an item within an order
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// Unique identifier for the order item
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the order
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Foreign key to the product
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Quantity of the product ordered
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price at the time of order (price may change over time)
        /// </summary>
        [Required]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total price for this line item (Quantity * UnitPrice)
        /// </summary>
        [Required]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Navigation property to the order
        /// </summary>
        public virtual Order Order { get; set; } = null!;

        /// <summary>
        /// Navigation property to the product
        /// </summary>
        public virtual Product Product { get; set; } = null!;
    }
}
