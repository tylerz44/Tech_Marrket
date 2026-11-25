using System.ComponentModel.DataAnnotations;

namespace TechMarket.Models;

public class UserCoupon
{
    public int Id { get; set; }
    [Required] public int DiscountCodeId { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public DateTime ClaimedAt { get; set; } = DateTime.Now;
    public DateTime? UsedAt { get; set; }
    public int? OrderId { get; set; }
    public DiscountCode DiscountCode { get; set; } = null!;
}
