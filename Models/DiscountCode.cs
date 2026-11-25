using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models;

public class DiscountCode
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(20)] public string DiscountType { get; set; } = "PERCENT"; // PERCENT hoặc FIXED
    [Range(0.01, 10000000000)] public decimal DiscountValue { get; set; }
    [Range(0, 10000000000)] public decimal MaxDiscountAmount { get; set; }
    [Range(0, 10000000000)] public decimal MinOrderAmount { get; set; }
    [Range(1, int.MaxValue)] public int UsageLimit { get; set; } = 100;
    public int UsedCount { get; set; }
    public DateTime StartAt { get; set; } = DateTime.Now;
    public DateTime EndAt { get; set; } = DateTime.Now.AddDays(30);
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
}
