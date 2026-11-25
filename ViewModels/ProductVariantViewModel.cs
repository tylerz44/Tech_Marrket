
using System.ComponentModel.DataAnnotations;

namespace TechMarket.ViewModels
{
    public class ProductVariantViewModel
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã SKU")]
        [StringLength(100)]
        [Display(Name = "Mã SKU")]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn kích cỡ")]
        [StringLength(50)]
        [Display(Name = "Kích cỡ")]
        public string Size { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn màu sắc")]
        [StringLength(100)]
        [Display(Name = "Màu sắc")]
        public string Color { get; set; } = string.Empty;

        [Range(0, int.MaxValue,
            ErrorMessage = "Tồn kho không được âm")]
        [Display(Name = "Số lượng tồn kho")]
        public int Quantity { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
    }
}
