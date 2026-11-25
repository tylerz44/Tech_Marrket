
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;

namespace TechMarket.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách sản phẩm dành cho khách hàng
        public async Task<IActionResult> Index(
            string? keyword,
            int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Quantity > 0)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    p.Name.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(products);
        }

        // Chi tiết sản phẩm
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.IsActive &&
                    p.Quantity > 0);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy các biến thể đang hoạt động và còn hàng
            var variants = await _context.ProductVariants
                .Where(v =>
                    v.ProductId == id &&
                    v.IsActive &&
                    v.Quantity > 0)
                .OrderBy(v => v.Size)
                .ThenBy(v => v.Color)
                .ToListAsync();

            ViewBag.Variants = variants;

            return View(product);
        }
    }
}
