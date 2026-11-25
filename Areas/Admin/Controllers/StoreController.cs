
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using StoreEntity = TechMarket.Models.Store;

namespace TechMarket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StoreController : Controller
{
    private readonly ApplicationDbContext _context;

    public StoreController(ApplicationDbContext context)
    {
        _context = context;
    }

    // DANH SÁCH CỬA HÀNG
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword)
    {
        var query =
            from s in _context.Stores
            join u in _context.Users
                on s.OwnerId equals u.Id
            select new StoreAdminRow
            {
                Id = s.Id,
                StoreName = s.StoreName,
                OwnerName = u.FullName,
                OwnerEmail = u.Email ?? "",
                Phone = s.Phone,
                Address = s.Address,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();

            query = query.Where(x =>
                x.StoreName.Contains(keyword) ||
                x.OwnerName.Contains(keyword) ||
                x.OwnerEmail.Contains(keyword));
        }

        ViewBag.Keyword = keyword;

        var stores = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(stores);
    }

    // HIỂN THỊ TRANG CHỈNH SỬA
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var store = await _context.Stores.FindAsync(id);

        if (store == null)
        {
            TempData["Error"] = "Không tìm thấy cửa hàng.";
            return RedirectToAction(nameof(Index));
        }

        return View(store);
    }

    // CẬP NHẬT CỬA HÀNG
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StoreEntity model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var store = await _context.Stores.FindAsync(id);

        if (store == null)
        {
            TempData["Error"] = "Không tìm thấy cửa hàng.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        store.StoreName = model.StoreName;
        store.Description = model.Description;
        store.Address = model.Address;
        store.Phone = model.Phone;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật thông tin cửa hàng thành công.";

        return RedirectToAction(nameof(Index));
    }

    // KHÓA / MỞ KHÓA CỬA HÀNG
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var store = await _context.Stores.FindAsync(id);

        if (store == null)
        {
            TempData["Error"] = "Không tìm thấy cửa hàng.";
            return RedirectToAction(nameof(Index));
        }

        store.IsActive = !store.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = store.IsActive
            ? "Đã kích hoạt cửa hàng thành công."
            : "Đã tạm ngưng cửa hàng.";

        return RedirectToAction(nameof(Index));
    }
}

// VIEW MODEL HIỂN THỊ DANH SÁCH
public class StoreAdminRow
{
    public int Id { get; set; }

    public string StoreName { get; set; } = "";

    public string OwnerName { get; set; } = "";

    public string OwnerEmail { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
