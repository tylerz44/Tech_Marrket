
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên người nhận")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD";

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái đơn hàng")]
        public string Status { get; set; } = "Chờ xác nhận";

        // Trạng thái thanh toán
        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Chưa thanh toán";

        // Mã giao dịch mô phỏng do TechMarket tạo
        [StringLength(100)]
        public string? PaymentTransactionNo { get; set; }

        // Mã tham chiếu của giao dịch Demo
        [StringLength(100)]
        public string? PaymentTransactionRef { get; set; }

        // Thời gian thanh toán thành công
        public DateTime? PaidAt { get; set; }

        [StringLength(40)]
        public string? DiscountCode { get; set; }

        [Range(0, 10000000000)]
        public decimal DiscountAmount { get; set; } = 0;

        [Range(0, 10000000000)]
        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<OrderDetail> OrderDetails { get; set; }
            = new List<OrderDetail>();
    }
}
