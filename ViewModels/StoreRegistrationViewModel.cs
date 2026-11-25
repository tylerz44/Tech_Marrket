
using System.ComponentModel.DataAnnotations;

namespace TechMarket.ViewModels
{
    public class StoreRegistrationViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng")]
        [StringLength(150)]
        public string StoreName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;
    }
}
