using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;
using TechMarket.ViewModels;

namespace TechMarket.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly string[] AllowedPaymentMethods =
        {
            "COD", "DEMOQR", "DEMOBANK", "DEMOCARD"
        };

        public CheckoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<Cart?> GetCartAsync(string userId)
        {
            return await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductVariant)
                .FirstOrDefaultAsync();
        }

        private async Task PrepareCheckoutView(Cart cart, string userId)
        {
            ViewBag.CartItems = cart.CartItems;
            ViewBag.TotalAmount = cart.CartItems.Sum(
                item => item.Product!.Price * item.Quantity);
            var userCoupons = await _context.UserCoupons
                .Where(x => x.UserId == userId && x.UsedAt == null)
                .Include(x => x.DiscountCode)
                .Where(x => x.DiscountCode.IsActive && x.DiscountCode.StartAt <= DateTime.Now && x.DiscountCode.EndAt >= DateTime.Now && x.DiscountCode.UsedCount < x.DiscountCode.UsageLimit)
                .Select(x => x.DiscountCode)
                .ToListAsync();
            ViewBag.UserCoupons = userCoupons;

            ViewBag.ShippingAddresses = await _context.ShippingAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var cart = await GetCartAsync(userId);
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            foreach (var item in cart.CartItems)
            {
                if (item.Product == null ||
                    item.ProductVariant == null ||
                    item.ProductVariant.ProductId != item.ProductId ||
                    !item.Product.IsActive ||
                    !item.ProductVariant.IsActive ||
                    item.ProductVariant.Quantity < item.Quantity)
                {
                    TempData["Error"] =
                        "Một số sản phẩm trong giỏ hàng không còn đủ tồn kho. Vui lòng kiểm tra lại.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            await PrepareCheckoutView(cart, userId);

            var defaultAddress = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

            var model = new CheckoutViewModel();
            if (defaultAddress != null)
            {
                model.ReceiverName = defaultAddress.ReceiverName;
                model.Phone = defaultAddress.Phone;
                model.ShippingAddress = defaultAddress.Address;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var cart = await GetCartAsync(userId);
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            if (!AllowedPaymentMethods.Contains(model.PaymentMethod))
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMethod),
                    "Phương thức thanh toán không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                await PrepareCheckoutView(cart, userId);
                return View("Index", model);
            }

            foreach (var item in cart.CartItems)
            {
                if (item.Product == null ||
                    item.ProductVariant == null ||
                    item.ProductVariant.ProductId != item.ProductId ||
                    !item.Product.IsActive ||
                    !item.ProductVariant.IsActive ||
                    item.ProductVariant.Quantity < item.Quantity)
                {
                    TempData["Error"] =
                        "Sản phẩm không còn đủ tồn kho hoặc đã ngừng kinh doanh. Vui lòng kiểm tra giỏ hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                bool storeIsActive = await _context.Stores.AnyAsync(s =>
                    s.Id == item.Product.StoreId && s.IsActive);

                if (!storeIsActive)
                {
                    TempData["Error"] =
                        "Một cửa hàng có sản phẩm trong giỏ hàng hiện không hoạt động.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                decimal subtotal = cart.CartItems.Sum(
                    item => item.Product!.Price * item.Quantity);
                decimal discountAmount = 0;
                DiscountCode? selectedCoupon = null;
                string? normalizedCouponCode = string.IsNullOrWhiteSpace(model.CouponCode)
                    ? null : model.CouponCode.Trim().ToUpperInvariant();

                if (normalizedCouponCode != null)
                {
                    selectedCoupon = await _context.DiscountCodes.FirstOrDefaultAsync(c =>
                        c.Code == normalizedCouponCode && c.IsActive &&
                        c.StartAt <= DateTime.Now && c.EndAt >= DateTime.Now &&
                        c.UsedCount < c.UsageLimit);

                    bool userOwnsCoupon = selectedCoupon != null &&
                        await _context.UserCoupons.AnyAsync(uc =>
                            uc.UserId == userId && uc.DiscountCodeId == selectedCoupon.Id &&
                            uc.UsedAt == null);

                    if (selectedCoupon == null || !userOwnsCoupon)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Mã giảm giá không hợp lệ, đã hết hạn hoặc bạn chưa nhận mã.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (subtotal < selectedCoupon.MinOrderAmount)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = $"Đơn hàng phải đạt tối thiểu {selectedCoupon.MinOrderAmount:N0}đ để sử dụng mã.";
                        return RedirectToAction(nameof(Index));
                    }

                    discountAmount = selectedCoupon.DiscountType == "PERCENT"
                        ? subtotal * selectedCoupon.DiscountValue / 100m
                        : selectedCoupon.DiscountValue;
                    if (selectedCoupon.DiscountType == "PERCENT" && selectedCoupon.MaxDiscountAmount > 0)
                        discountAmount = Math.Min(discountAmount, selectedCoupon.MaxDiscountAmount);
                    discountAmount = Math.Min(subtotal, Math.Round(discountAmount, 0, MidpointRounding.AwayFromZero));
                }

                decimal totalAmount = subtotal - discountAmount;
                bool isOnlinePayment = model.PaymentMethod != "COD";

                var order = new Order
                {
                    UserId = userId,
                    ReceiverName = model.ReceiverName,
                    Phone = model.Phone,
                    ShippingAddress = model.ShippingAddress,
                    Note = model.Note,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = isOnlinePayment
                        ? "Chờ thanh toán"
                        : "Chưa thanh toán",
                    Status = "Chờ xác nhận",
                    TotalAmount = totalAmount,
                    DiscountCode = selectedCoupon?.Code,
                    DiscountAmount = discountAmount,
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                if (isOnlinePayment)
                {
                    order.PaymentTransactionRef =
                        $"TM-{order.Id}-{Guid.NewGuid():N}".ToUpperInvariant();
                }

                foreach (var item in cart.CartItems)
                {
                    var product = item.Product!;
                    var variant = item.ProductVariant!;

                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        ProductVariantId = variant.Id,
                        StoreId = product.StoreId,
                        ProductName = product.Name,
                        SKU = variant.SKU,
                        Size = variant.Size,
                        Color = variant.Color,
                        UnitPrice = product.Price,
                        Quantity = item.Quantity,
                        TotalPrice = product.Price * item.Quantity
                    });
                }

                await _context.SaveChangesAsync();

                var storeIds = cart.CartItems
                    .Select(item => item.Product!.StoreId)
                    .Distinct()
                    .ToList();

                foreach (var storeId in storeIds)
                {
                    _context.StoreOrders.Add(new StoreOrder
                    {
                        OrderId = order.Id,
                        StoreId = storeId,
                        Status = "Chờ xác nhận",
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();

                // Chỉ trừ tồn kho một lần khi tạo đơn hàng.
                foreach (var item in cart.CartItems)
                {
                    int affectedRows = await _context.ProductVariants
                        .Where(v =>
                            v.Id == item.ProductVariantId &&
                            v.ProductId == item.ProductId &&
                            v.IsActive &&
                            v.Quantity >= item.Quantity)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(v => v.Quantity,
                                v => v.Quantity - item.Quantity));

                    if (affectedRows == 0)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] =
                            "Tồn kho vừa thay đổi. Vui lòng kiểm tra lại giỏ hàng.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                var productIds = cart.CartItems
                    .Select(item => item.ProductId)
                    .Distinct()
                    .ToList();

                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in products)
                {
                    product.Quantity = await _context.ProductVariants
                        .Where(v => v.ProductId == product.Id)
                        .SumAsync(v => (int?)v.Quantity) ?? 0;
                }

                if (selectedCoupon != null)
                {
                    // Đánh dấu voucher đã dùng đúng một lần trong cùng transaction.
                    var couponRow = await _context.DiscountCodes
                        .Where(c => c.Id == selectedCoupon.Id && c.IsActive &&
                                    c.UsedCount < c.UsageLimit &&
                                    c.StartAt <= DateTime.Now && c.EndAt >= DateTime.Now)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(c => c.UsedCount, c => c.UsedCount + 1));
                    if (couponRow == 0)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Mã giảm giá vừa hết lượt sử dụng. Vui lòng thử mã khác.";
                        return RedirectToAction(nameof(Index));
                    }

                    var userCoupon = await _context.UserCoupons.FirstOrDefaultAsync(x =>
                        x.UserId == userId && x.DiscountCodeId == selectedCoupon.Id && x.UsedAt == null);
                    if (userCoupon == null)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Voucher không còn trong kho mã của bạn.";
                        return RedirectToAction(nameof(Index));
                    }
                    userCoupon.UsedAt = DateTime.Now;
                    userCoupon.OrderId = order.Id;
                }

                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                if (isOnlinePayment)
                {
                    return RedirectToAction(
                        "Pay", "DemoPayment", new { id = order.Id });
                }

                TempData["Success"] =
                    "Đặt hàng thành công! Bạn sẽ thanh toán khi nhận hàng.";

                return RedirectToAction("Index", "OrderHistory");
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] =
                    "Đã xảy ra lỗi khi đặt hàng. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }
    }
}
