
using System;
using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models
{
    public class Product
    {
        // Liên kết với danh mục sản phẩm
        public Category? Category { get; set; }
        public int Id { get; set; }

        // Cửa hàng sở hữu sản phẩm
        [Required]
        [Display(Name = "Cửa hàng")]
        public int StoreId { get; set; }

        // Danh mục sản phẩm
        [Required]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        // Tên sản phẩm
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        // Mô tả sản phẩm
        [StringLength(2000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // Giá bán
        [Required]
        [Range(0.01, 1000000000,
            ErrorMessage = "Giá bán phải lớn hơn 0")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        // Số lượng tồn kho
        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng không được âm")]
        [Display(Name = "Số lượng tồn kho")]
        public int Quantity { get; set; }

        // Kích cỡ: S, M, L, XL...
        [StringLength(200)]
        [Display(Name = "Kích cỡ")]
        public string? Size { get; set; }

        // Màu sắc: Đen, Trắng, Xanh...
        [StringLength(200)]
        [Display(Name = "Màu sắc")]
        public string? Color { get; set; }

        // Đường dẫn hình ảnh
        [StringLength(500)]
        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        // Trạng thái kinh doanh
        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        // Ngày tạo sản phẩm
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
