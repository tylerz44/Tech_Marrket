
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy ID khách hàng đang đăng nhập
        private string GetUserId()
        {
            return User.FindFirstValue(
                ClaimTypes.NameIdentifier)!;
        }

        // Lấy giỏ hàng, bao gồm sản phẩm và biến thể
        private async Task<Cart> GetOrCreateCart()
        {
            string userId = GetUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductVariant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // GET: /Cart/Index
        public async Task<IActionResult> Index()
        {
            var cart = await GetOrCreateCart();

            return View(cart);
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            int variantId,
            int quantity = 1)
        {
            if (quantity < 1)
            {
                TempData["Error"] =
                    "Số lượng phải lớn hơn 0.";

                return RedirectToAction(
                    "Index", "Product");
            }

            // Tìm biến thể và sản phẩm tương ứng
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null ||
                !variant.IsActive ||
                variant.Product == null ||
                !variant.Product.IsActive)
            {
                TempData["Error"] =
                    "Biến thể không tồn tại hoặc đã ngừng bán.";

                return RedirectToAction(
                    "Index", "Product");
            }

            var product = variant.Product;

            if (variant.Quantity < 1)
            {
                TempData["Error"] =
                    "Biến thể này đã hết hàng.";

                return RedirectToAction(
                    "Details", "Product",
                    new { id = product.Id });
            }

            var cart = await GetOrCreateCart();

            // Tìm đúng dòng giỏ hàng theo biến thể
            var cartItem = cart.CartItems
                .FirstOrDefault(ci =>
                    ci.ProductVariantId == variantId);

            int currentQuantity =
                cartItem?.Quantity ?? 0;

            int newQuantity = currentQuantity + quantity;

            // Kiểm tra tồn kho riêng của biến thể
            if (newQuantity > variant.Quantity)
            {
                TempData["Error"] =
                    $"Biến thể này chỉ còn {variant.Quantity} sản phẩm.";

                return RedirectToAction(
                    "Details", "Product",
                    new { id = product.Id });
            }

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = product.Id,
                    ProductVariantId = variant.Id,
                    Quantity = quantity
                };

                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity = newQuantity;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã thêm sản phẩm vào giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            int id,
            int quantity)
        {
            string userId = GetUserId();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .Include(ci => ci.ProductVariant)
                .FirstOrDefaultAsync(ci =>
                    ci.Id == id &&
                    ci.Cart != null &&
                    ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return NotFound();
            }

            if (quantity < 1)
            {
                TempData["Error"] =
                    "Số lượng phải lớn hơn 0.";

                return RedirectToAction(nameof(Index));
            }

            var product = cartItem.Product;
            var variant = cartItem.ProductVariant;

            if (product == null ||
                variant == null ||
                !product.IsActive ||
                !variant.IsActive)
            {
                TempData["Error"] =
                    "Sản phẩm hoặc biến thể đã ngừng bán.";

                return RedirectToAction(nameof(Index));
            }

            if (quantity > variant.Quantity)
            {
                TempData["Error"] =
                    $"Biến thể chỉ còn {variant.Quantity} sản phẩm.";

                return RedirectToAction(nameof(Index));
            }

            cartItem.Quantity = quantity;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã cập nhật số lượng.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            string userId = GetUserId();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci =>
                    ci.Id == id &&
                    ci.Cart != null &&
                    ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(cartItem);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã xóa sản phẩm khỏi giỏ hàng.";

            return RedirectToAction(nameof(Index));
        }
    }
}
