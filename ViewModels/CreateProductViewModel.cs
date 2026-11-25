
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechMarket.ViewModels
{
    public class CreateProductViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Range(0.01, 1000000000,
            ErrorMessage = "Giá bán phải lớn hơn 0")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng không được âm")]
        [Display(Name = "Số lượng tồn kho")]
        public int Quantity { get; set; }

        [StringLength(200)]
        [Display(Name = "Kích cỡ")]
        public string? Size { get; set; }

        [StringLength(200)]
        [Display(Name = "Màu sắc")]
        public string? Color { get; set; }

        [Display(Name = "Hình ảnh sản phẩm")]
        public IFormFile? ImageFile { get; set; }

        public List<SelectListItem> Categories { get; set; }
            = new List<SelectListItem>();
    }
}

