
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Category
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(categories);
        }

        // GET: /Admin/Category/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Category/Create


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            // Kiểm tra dữ liệu nhận được
            Console.WriteLine("========== CATEGORY DEBUG ==========");
            Console.WriteLine("Name: " + category.Name);
            Console.WriteLine("Description: " + category.Description);
            Console.WriteLine("ModelState: " + ModelState.IsValid);

            // Kiểm tra lỗi dữ liệu
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value!.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                ViewBag.ErrorMessage = string.Join(" | ", errors);

                return View(category);
            }

            try
            {
                category.CreatedAt = DateTime.Now;
                category.IsActive = true;

                _context.Categories.Add(category);

                // Lưu dữ liệu
                int result = await _context.SaveChangesAsync();

                Console.WriteLine("Rows affected: " + result);

                TempData["Success"] = "Thêm danh mục thành công!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.ToString();

                return View(category);
            }
        }

        // GET: /Admin/Category/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: /Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var existingCategory = await _context.Categories.FindAsync(id);

            if (existingCategory == null)
            {
                return NotFound();
            }

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Category/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            category.IsActive = !category.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = category.IsActive
                ? "Đã kích hoạt danh mục."
                : "Đã ẩn danh mục.";

            return RedirectToAction(nameof(Index));
        }
    }
}

