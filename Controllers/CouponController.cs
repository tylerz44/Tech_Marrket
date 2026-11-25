using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Controllers;

[Authorize(Roles = "Customer")]
public class CouponController : Controller
{
    private readonly ApplicationDbContext _context;
    public CouponController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var now = DateTime.Now;
        var coupons = await _context.DiscountCodes.Where(x => x.IsActive && x.StartAt <= now && x.EndAt >= now && x.UsedCount < x.UsageLimit).OrderBy(x => x.EndAt).ToListAsync();
        var claimed = await _context.UserCoupons.Where(x => x.UserId == userId).Select(x => x.DiscountCodeId).ToListAsync();
        ViewBag.ClaimedIds = claimed;
        return View(coupons);
    }

    [HttpGet]
    public async Task<IActionResult> MyCoupons()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var coupons = await _context.UserCoupons
            .Where(x => x.UserId == userId)
            .Include(x => x.DiscountCode)
            .OrderByDescending(x => x.ClaimedAt)
            .ToListAsync();
        return View(coupons);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var coupon = await _context.DiscountCodes.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.StartAt <= DateTime.Now && x.EndAt >= DateTime.Now && x.UsedCount < x.UsageLimit);
        if (coupon == null) { TempData["Error"] = "Mã giảm giá không còn khả dụng."; return RedirectToAction(nameof(Index)); }
        if (await _context.UserCoupons.AnyAsync(x => x.UserId == userId && x.DiscountCodeId == id)) { TempData["Error"] = "Bạn đã nhận mã này rồi."; return RedirectToAction(nameof(Index)); }
        _context.UserCoupons.Add(new UserCoupon { UserId = userId, DiscountCodeId = id, ClaimedAt = DateTime.Now });
        await _context.SaveChangesAsync(); TempData["Success"] = "Nhận mã giảm giá thành công."; return RedirectToAction(nameof(Index));
    }
}
