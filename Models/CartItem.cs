
using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;

        // Navigation Properties
        public Cart? Cart { get; set; }

        public Product? Product { get; set; }

        public ProductVariant? ProductVariant { get; set; }
    }
}
