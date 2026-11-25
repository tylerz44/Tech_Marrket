using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    public ProductController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? categoryId, int? storeId)
    {
        var query = _context.Products.Include(p => p.Category).Join(_context.Stores, p => p.StoreId, s => s.Id, (p, s) => new ProductAdminRow { Product = p, StoreName = s.StoreName }).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Product.Name.Contains(keyword));
        if (categoryId.HasValue) query = query.Where(x => x.Product.CategoryId == categoryId.Value);
        if (storeId.HasValue) query = query.Where(x => x.Product.StoreId == storeId.Value);
        ViewBag.Keyword = keyword; ViewBag.CategoryId = categoryId; ViewBag.StoreId = storeId;
        ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(x => x.Name).ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Stores = new SelectList(await _context.Stores.OrderBy(x => x.StoreName).ToListAsync(), "Id", "StoreName", storeId);
        return View(await query.OrderByDescending(x => x.Product.CreatedAt).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLists();
        return View(new Product { IsActive = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model)
    {
        ModelState.Remove(nameof(Product.Category));
        if (!await _context.Stores.AnyAsync(x => x.Id == model.StoreId)) ModelState.AddModelError(nameof(model.StoreId), "Cửa hàng không tồn tại.");
        if (!await _context.Categories.AnyAsync(x => x.Id == model.CategoryId && x.IsActive)) ModelState.AddModelError(nameof(model.CategoryId), "Vui lòng chọn danh mục đang hoạt động.");
        if (!ModelState.IsValid) { await LoadLists(model.CategoryId, model.StoreId); return View(model); }
        model.CreatedAt = DateTime.Now;
        _context.Products.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã thêm sản phẩm vào danh mục và cửa hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        await LoadLists(product.CategoryId, product.StoreId);
        return View(product);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product model)
    {
        if (id != model.Id) return BadRequest();
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        ModelState.Remove(nameof(Product.Category));
        if (!await _context.Stores.AnyAsync(x => x.Id == model.StoreId)) ModelState.AddModelError(nameof(model.StoreId), "Cửa hàng không tồn tại.");
        if (!await _context.Categories.AnyAsync(x => x.Id == model.CategoryId)) ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không tồn tại.");
        if (!ModelState.IsValid) { await LoadLists(model.CategoryId, model.StoreId); return View(model); }
        product.StoreId = model.StoreId; product.CategoryId = model.CategoryId; product.Name = model.Name; product.Description = model.Description; product.Price = model.Price; product.Quantity = model.Quantity; product.Size = model.Size; product.Color = model.Color; product.ImageUrl = model.ImageUrl; product.IsActive = model.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.IsActive = !product.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = product.IsActive ? "Đã hiển thị sản phẩm." : "Đã ẩn sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLists(int? categoryId = null, int? storeId = null)
    {
        ViewBag.Categories = new SelectList(await _context.Categories.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Stores = new SelectList(await _context.Stores.OrderBy(x => x.StoreName).ToListAsync(), "Id", "StoreName", storeId);
    }
}

public class ProductAdminRow
{
    public Product Product { get; set; } = null!;
    public string StoreName { get; set; } = "";
}
