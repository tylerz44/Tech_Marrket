
using System;
using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        // Sản phẩm cha
        [Required]
        public int ProductId { get; set; }

        // Mã SKU riêng của từng biến thể
        [Required(ErrorMessage = "Vui lòng nhập mã SKU")]
        [StringLength(100)]
        public string SKU { get; set; } = string.Empty;

        // Kích cỡ: S, M, L, XL...
        [Required(ErrorMessage = "Vui lòng chọn kích cỡ")]
        [StringLength(50)]
        public string Size { get; set; } = string.Empty;

        // Màu sắc: Đen, Trắng, Xanh...
        [Required(ErrorMessage = "Vui lòng chọn màu sắc")]
        [StringLength(100)]
        public string Color { get; set; } = string.Empty;

        // Số lượng tồn kho riêng của biến thể
        [Range(0, int.MaxValue,
            ErrorMessage = "Tồn kho không được âm")]
        public int Quantity { get; set; }

        // Trạng thái kinh doanh
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public Product? Product { get; set; }
    }
}

