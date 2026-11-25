using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DiscountController : Controller
{
    private readonly ApplicationDbContext _context;
    public DiscountController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index() => View(await _context.DiscountCodes.OrderByDescending(x => x.CreatedAt).ToListAsync());

    [HttpGet] public IActionResult Create() => View(new DiscountCode());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscountCode model)
    {
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (await _context.DiscountCodes.AnyAsync(x => x.Code == model.Code))
            ModelState.AddModelError(nameof(model.Code), "Mã giảm giá đã tồn tại.");
        if (model.EndAt <= model.StartAt) ModelState.AddModelError(nameof(model.EndAt), "Ngày kết thúc phải sau ngày bắt đầu.");
        if (model.DiscountType == "PERCENT" && model.DiscountValue > 100) ModelState.AddModelError(nameof(model.DiscountValue), "Phần trăm giảm không được vượt quá 100.");
        if (model.DiscountType != "PERCENT" && model.DiscountType != "FIXED") ModelState.AddModelError(nameof(model.DiscountType), "Loại giảm giá không hợp lệ.");
        if (!ModelState.IsValid) return View(model);
        model.CreatedAt = DateTime.Now; model.UsedCount = 0;
        _context.DiscountCodes.Add(model); await _context.SaveChangesAsync();
        TempData["Success"] = "Tạo mã giảm giá thành công."; return RedirectToAction(nameof(Index));
    }

    [HttpGet] public async Task<IActionResult> Edit(int id)
    { var item = await _context.DiscountCodes.FindAsync(id); return item == null ? NotFound() : View(item); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DiscountCode model)
    {
        if (id != model.Id) return BadRequest();
        model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (await _context.DiscountCodes.AnyAsync(x => x.Code == model.Code && x.Id != id)) ModelState.AddModelError(nameof(model.Code), "Mã đã tồn tại.");
        if (model.EndAt <= model.StartAt) ModelState.AddModelError(nameof(model.EndAt), "Ngày kết thúc phải sau ngày bắt đầu.");
        if (model.DiscountType == "PERCENT" && model.DiscountValue > 100) ModelState.AddModelError(nameof(model.DiscountValue), "Phần trăm giảm không được vượt quá 100.");
        if (model.DiscountType != "PERCENT" && model.DiscountType != "FIXED") ModelState.AddModelError(nameof(model.DiscountType), "Loại giảm giá không hợp lệ.");
        if (!ModelState.IsValid) return View(model);
        var entity = await _context.DiscountCodes.FindAsync(id); if (entity == null) return NotFound();
        entity.Code = model.Code; entity.DiscountType = model.DiscountType; entity.DiscountValue = model.DiscountValue;
        entity.MaxDiscountAmount = model.MaxDiscountAmount; entity.MinOrderAmount = model.MinOrderAmount;
        entity.UsageLimit = model.UsageLimit; entity.StartAt = model.StartAt; entity.EndAt = model.EndAt; entity.IsActive = model.IsActive;
        await _context.SaveChangesAsync(); TempData["Success"] = "Cập nhật mã giảm giá thành công."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    { var item = await _context.DiscountCodes.FindAsync(id); if (item == null) return NotFound(); item.IsActive = !item.IsActive; await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
}
