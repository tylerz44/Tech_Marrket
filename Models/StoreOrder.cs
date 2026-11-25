
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechMarket.Models
{
    public class StoreOrder
    {
        public int Id { get; set; }

        // Đơn hàng tổng của khách hàng
        public int OrderId { get; set; }

        // Cửa hàng nhận đơn hàng
        public int StoreId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Chờ xác nhận";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Order Order { get; set; } = null!;

        public Store Store { get; set; } = null!;
    }
}
