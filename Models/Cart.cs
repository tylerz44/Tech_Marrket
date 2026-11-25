
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Một giỏ hàng có nhiều sản phẩm
        public ICollection<CartItem> CartItems { get; set; }
            = new List<CartItem>();
    }
}
