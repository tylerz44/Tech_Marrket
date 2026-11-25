
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using TechMarket.Data;
using TechMarket.Models;
using TechMarket.ViewModels;

namespace TechMarket.Areas.Store.Controllers
{
    [Area("Store")]
    [Authorize(Roles = "StoreOwner")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ProductController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // =========================================
        // 1. DANH SÁCH SẢN PHẨM
        // =========================================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            var products = await _context.Products
                .Where(p => p.StoreId == store.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.StoreName = store.StoreName;

            return View(products);
        }

        // =========================================
        // 2. THÊM SẢN PHẨM - GET
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateProductViewModel();

            await LoadCategories(model);

            return View(model);
        }

        // =========================================
        // 3. THÊM SẢN PHẨM - POST
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == model.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục không hợp lệ hoặc đã bị ẩn.");
            }

            if (model.ImageFile != null &&
                !IsAllowedImage(model.ImageFile))
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    "Ảnh phải là JPG, JPEG, PNG hoặc WEBP và không quá 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories(model);
                return View(model);
            }

            string? imageUrl = null;

            try
            {
                if (model.ImageFile != null)
                {
                    imageUrl = await SaveImage(model.ImageFile);
                }

                var product = new Product
                {
                    StoreId = store.Id,
                    CategoryId = model.CategoryId,
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    Size = model.Size,
                    Color = model.Color,
                    ImageUrl = imageUrl,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Products.Add(product);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm sản phẩm thành công!";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                if (!string.IsNullOrEmpty(imageUrl))
                    DeleteImage(imageUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể lưu sản phẩm. Vui lòng thử lại.");

                await LoadCategories(model);

                return View(model);
            }
        }

        // =========================================
        // 4. CHỈNH SỬA SẢN PHẨM - GET
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.StoreId == store.Id);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            var model = new EditProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                Price = product.Price,
                Quantity = product.Quantity,
                Size = product.Size,
                Color = product.Color,
                CurrentImageUrl = product.ImageUrl
            };

            await LoadEditCategories(model, product.CategoryId);

            return View(model);
        }

        // =========================================
        // 5. CHỈNH SỬA SẢN PHẨM - POST
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            EditProductViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.StoreId == store.Id);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == model.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Danh mục không hợp lệ hoặc đã bị ẩn.");
            }

            if (model.ImageFile != null &&
                !IsAllowedImage(model.ImageFile))
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    "Ảnh phải là JPG, JPEG, PNG hoặc WEBP và không quá 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                model.CurrentImageUrl = product.ImageUrl;

                await LoadEditCategories(model, model.CategoryId);

                return View(model);
            }

            string? oldImageUrl = product.ImageUrl;
            string? newImageUrl = null;

            try
            {
                if (model.ImageFile != null)
                {
                    newImageUrl = await SaveImage(model.ImageFile);
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.CategoryId = model.CategoryId;
                product.Price = model.Price;
                product.Quantity = model.Quantity;
                product.Size = model.Size;
                product.Color = model.Color;

                // Chỉ thay ảnh khi người dùng chọn ảnh mới
                if (newImageUrl != null)
                {
                    product.ImageUrl = newImageUrl;
                }

                await _context.SaveChangesAsync();

                // Xóa ảnh cũ sau khi lưu database thành công
                if (newImageUrl != null &&
                    !string.IsNullOrEmpty(oldImageUrl))
                {
                    DeleteImage(oldImageUrl);
                }

                TempData["Success"] = "Cập nhật sản phẩm thành công!";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Nếu cập nhật thất bại thì xóa ảnh mới đã upload
                if (!string.IsNullOrEmpty(newImageUrl))
                {
                    DeleteImage(newImageUrl);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể cập nhật sản phẩm. Vui lòng thử lại.");

                model.CurrentImageUrl = oldImageUrl;

                await LoadEditCategories(model, model.CategoryId);

                return View(model);
            }
        }

        // =========================================
        // 6. ẨN / HIỆN SẢN PHẨM
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            // Chỉ được thay đổi sản phẩm thuộc cửa hàng của mình
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.StoreId == store.Id);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            product.IsActive = !product.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = product.IsActive
                ? "Đã hiển thị sản phẩm thành công!"
                : "Đã ẩn sản phẩm thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // 7. TẢI DANH MỤC CHO TRANG CREATE
        // =========================================
        private async Task LoadCategories(CreateProductViewModel model)
        {
            model.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

        // =========================================
        // 8. TẢI DANH MỤC CHO TRANG EDIT
        // =========================================
        private async Task LoadEditCategories(
            EditProductViewModel model,
            int selectedCategoryId)
        {
            model.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == selectedCategoryId
                })
                .ToListAsync();
        }

        // =========================================
        // 9. KIỂM TRA ẢNH
        // =========================================
        private bool IsAllowedImage(IFormFile file)
        {
            if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
                return false;

            string extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            return allowedExtensions.Contains(extension);
        }

        // =========================================
        // 10. LƯU ẢNH
        // =========================================
        private async Task<string> SaveImage(IFormFile file)
        {
            string uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "images",
                "products");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            string fileName = Guid.NewGuid().ToString("N") + extension;

            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/images/products/" + fileName;
        }

        // =========================================
        // 11. XÓA ẢNH
        // =========================================
        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            const string imagePrefix = "/images/products/";

            if (!imageUrl.StartsWith(
                imagePrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string fileName = Path.GetFileName(imageUrl);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            string filePath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "products",
                fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // =========================================
        // 12. DANH SÁCH BIẾN THỂ SẢN PHẨM
        // =========================================
        [HttpGet]
        public async Task<IActionResult> Variants(int productId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            // Kiểm tra sản phẩm có thuộc cửa hàng hiện tại không
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == productId &&
                    p.StoreId == store.Id);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .OrderBy(v => v.Size)
                .ThenBy(v => v.Color)
                .ToListAsync();

            ViewBag.Product = product;

            return View("Variants/Index", variants);
        }

        // =========================================
        // 13. THÊM BIẾN THỂ - GET
        // =========================================
        [HttpGet]
        public async Task<IActionResult> CreateVariant(int productId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == productId &&
                    p.StoreId == store.Id);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            var model = new ProductVariantViewModel
            {
                ProductId = productId,
                IsActive = true
            };

            ViewBag.ProductName = product.Name;

            return View("Variants/Create", model);
        }

        // =========================================
        // 14. THÊM BIẾN THỂ - POST
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVariant(
            ProductVariantViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == model.ProductId &&
                    p.StoreId == store.Id);

            if (product == null)
                return NotFound("Không tìm thấy sản phẩm.");

            ViewBag.ProductName = product.Name;

            // Kiểm tra dữ liệu đầu vào
            if (!ModelState.IsValid)
                return View("Variants/Create", model);

            model.SKU = model.SKU.Trim();
            model.Size = model.Size.Trim();
            model.Color = model.Color.Trim();

            // Kiểm tra SKU đã tồn tại
            bool skuExists = await _context.ProductVariants
                .AnyAsync(v => v.SKU == model.SKU);

            if (skuExists)
            {
                ModelState.AddModelError(
                    nameof(model.SKU),
                    "Mã SKU đã tồn tại.");

                return View("Variants/Create", model);
            }

            // Kiểm tra size và màu đã tồn tại trong sản phẩm
            bool variantExists = await _context.ProductVariants
                .AnyAsync(v =>
                    v.ProductId == model.ProductId &&
                    v.Size == model.Size &&
                    v.Color == model.Color);

            if (variantExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Biến thể có kích cỡ và màu sắc này đã tồn tại.");

                return View("Variants/Create", model);
            }

            var variant = new ProductVariant
            {
                ProductId = model.ProductId,
                SKU = model.SKU,
                Size = model.Size,
                Color = model.Color,
                Quantity = model.Quantity,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.ProductVariants.Add(variant);

            try
            {
                await _context.SaveChangesAsync();

                // Đồng bộ tồn kho sản phẩm
                await SyncProductQuantity(model.ProductId);

                TempData["Success"] = "Thêm biến thể thành công!";

                return RedirectToAction(
                    nameof(Variants),
                    new { productId = model.ProductId });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể lưu biến thể. SKU hoặc tổ hợp size và màu có thể đã tồn tại.");

                return View("Variants/Create", model);
            }
        }

        // =========================================
        // 15. CHỈNH SỬA BIẾN THỂ - GET
        // =========================================
        [HttpGet]
        public async Task<IActionResult> EditVariant(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    v.Product != null &&
                    v.Product.StoreId == store.Id);

            if (variant == null)
                return NotFound("Không tìm thấy biến thể.");

            var model = new ProductVariantViewModel
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                SKU = variant.SKU,
                Size = variant.Size,
                Color = variant.Color,
                Quantity = variant.Quantity,
                IsActive = variant.IsActive
            };

            ViewBag.ProductName = variant.Product!.Name;

            return View("Variants/Edit", model);
        }

        // =========================================
        // 16. CHỈNH SỬA BIẾN THỂ - POST
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVariant(
            int id,
            ProductVariantViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    v.Product != null &&
                    v.Product.StoreId == store.Id);

            if (variant == null)
                return NotFound("Không tìm thấy biến thể.");

            ViewBag.ProductName = variant.Product!.Name;

            if (!ModelState.IsValid)
                return View("Variants/Edit", model);

            model.SKU = model.SKU.Trim();
            model.Size = model.Size.Trim();
            model.Color = model.Color.Trim();

            // Kiểm tra SKU trùng với biến thể khác
            bool skuExists = await _context.ProductVariants
                .AnyAsync(v =>
                    v.SKU == model.SKU &&
                    v.Id != id);

            if (skuExists)
            {
                ModelState.AddModelError(
                    nameof(model.SKU),
                    "Mã SKU đã được sử dụng.");

                return View("Variants/Edit", model);
            }

            // Kiểm tra tổ hợp size và màu trùng
            bool variantExists = await _context.ProductVariants
                .AnyAsync(v =>
                    v.ProductId == variant.ProductId &&
                    v.Size == model.Size &&
                    v.Color == model.Color &&
                    v.Id != id);

            if (variantExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Size và màu sắc này đã tồn tại trong sản phẩm.");

                return View("Variants/Edit", model);
            }

            variant.SKU = model.SKU;
            variant.Size = model.Size;
            variant.Color = model.Color;
            variant.Quantity = model.Quantity;
            variant.IsActive = model.IsActive;

            try
            {
                await _context.SaveChangesAsync();

                // Đồng bộ tồn kho sản phẩm
                await SyncProductQuantity(variant.ProductId);

                TempData["Success"] = "Cập nhật biến thể thành công!";

                return RedirectToAction(
                    nameof(Variants),
                    new { productId = variant.ProductId });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể cập nhật biến thể. SKU hoặc tổ hợp size và màu có thể đã tồn tại.");

                return View("Variants/Edit", model);
            }
        }

        // =========================================
        // 17. ẨN / HIỆN BIẾN THỂ
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVariantStatus(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var store = await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (store == null)
                return NotFound("Không tìm thấy cửa hàng.");

            // Chỉ cho phép thay đổi biến thể thuộc cửa hàng của mình
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v =>
                    v.Id == id &&
                    v.Product != null &&
                    v.Product.StoreId == store.Id);

            if (variant == null)
                return NotFound("Không tìm thấy biến thể.");

            variant.IsActive = !variant.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = variant.IsActive
                ? "Đã hiển thị biến thể thành công!"
                : "Đã ẩn biến thể thành công!";

            return RedirectToAction(
                nameof(Variants),
                new { productId = variant.ProductId });
        }

        // =========================================
        // 18. ĐỒNG BỘ TỒN KHO SẢN PHẨM
        // =========================================
        private async Task SyncProductQuantity(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return;

            // Tổng tồn kho của tất cả biến thể,
            // bao gồm cả biến thể đang ẩn
            product.Quantity = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .SumAsync(v => (int?)v.Quantity) ?? 0;

            await _context.SaveChangesAsync();
        }

    }
}
