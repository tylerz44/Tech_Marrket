
using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        // Đơn hàng
        [Required]
        public int OrderId { get; set; }

        // Sản phẩm gốc
        [Required]
        public int ProductId { get; set; }

        // Biến thể đã mua
        [Required]
        public int ProductVariantId { get; set; }

        // Cửa hàng bán sản phẩm
        [Required]
        public int StoreId { get; set; }

        // Lưu thông tin sản phẩm tại thời điểm mua
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Size { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Color { get; set; } = string.Empty;

        // Giá bán tại thời điểm đặt hàng
        [Range(0.01, 1000000000)]
        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }

        // Navigation properties
        public Order? Order { get; set; }

        public Product? Product { get; set; }

        public ProductVariant? ProductVariant { get; set; }

        public Store? Store { get; set; }
    }
}
