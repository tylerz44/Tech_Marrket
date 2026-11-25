
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrderHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly string[] AllowedPaymentMethods =
        {
            "COD",
            "DEMOQR",
            "DEMOBANK",
            "DEMOCARD"
        };

        public OrderHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Đồng bộ trạng thái Order từ các StoreOrder
        private async Task<bool> SyncOrderStatusAsync(Order order)
        {
            var statuses = await _context.StoreOrders
                .Where(so => so.OrderId == order.Id)
                .Select(so => so.Status)
                .ToListAsync();

            if (!statuses.Any())
                return false;

            string newStatus;

            if (statuses.All(s => s == "Đã giao hàng"))
            {
                newStatus = "Đã giao hàng";
            }
            else if (statuses.All(s => s == "Đã hủy"))
            {
                newStatus = "Đã hủy";
            }
            else if (statuses.Any(s =>
                s == "Đang giao hàng" ||
                s == "Đã giao hàng"))
            {
                newStatus = "Đang giao hàng";
            }
            else
            {
                newStatus = "Chờ xác nhận";
            }

            if (order.Status == newStatus)
                return false;

            order.Status = newStatus;
            return true;
        }

        // GET: OrderHistory/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Đồng bộ trạng thái các đơn hàng
            bool hasChanges = false;

            foreach (var order in orders)
            {
                if (await SyncOrderStatusAsync(order))
                    hasChanges = true;
            }

            if (hasChanges)
                await _context.SaveChangesAsync();

            return View(orders);
        }

        // GET: OrderHistory/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var order = await _context.Orders
                .Where(o => o.Id == id && o.UserId == userId)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            // Đồng bộ trạng thái trước khi hiển thị
            bool hasChanges = await SyncOrderStatusAsync(order);

            if (hasChanges)
                await _context.SaveChangesAsync();

            return View(order);
        }

        // POST: OrderHistory/ChangePaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePaymentMethod(
            int id,
            string paymentMethod)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            if (!AllowedPaymentMethods.Contains(paymentMethod))
            {
                TempData["Error"] =
                    "Phương thức thanh toán không hợp lệ.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            // Đồng bộ trạng thái mới nhất trước khi đổi thanh toán
            await SyncOrderStatusAsync(order);

            if (order.Status != "Chờ xác nhận")
            {
                TempData["Error"] =
                    "Chỉ có thể đổi phương thức thanh toán khi đơn hàng đang chờ xác nhận.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            if (order.PaymentStatus == "Đã thanh toán")
            {
                TempData["Error"] =
                    "Đơn hàng đã thanh toán nên không thể đổi phương thức.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            if (order.PaymentStatus == "Đã hủy")
            {
                TempData["Error"] =
                    "Đơn hàng đã bị hủy thanh toán, không thể đổi phương thức.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            order.PaymentMethod = paymentMethod;

            order.PaymentStatus = paymentMethod == "COD"
                ? "Chưa thanh toán"
                : "Chờ thanh toán";

            order.PaymentTransactionRef = paymentMethod == "COD"
                ? null
                : $"TM-{order.Id}-{Guid.NewGuid():N}".ToUpperInvariant();

            order.PaymentTransactionNo = null;
            order.PaidAt = null;

            await _context.SaveChangesAsync();

            if (paymentMethod == "COD")
            {
                TempData["Success"] =
                    "Đã đổi phương thức sang thanh toán khi nhận hàng.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            TempData["Success"] =
                "Đã đổi phương thức thanh toán. Vui lòng hoàn tất thanh toán Demo.";

            return RedirectToAction(
                "Pay", "DemoPayment", new { id });
        }

        // POST: OrderHistory/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o =>
                    o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            // Đồng bộ trạng thái mới nhất trước khi kiểm tra hủy
            await SyncOrderStatusAsync(order);

            if (order.PaymentStatus == "Đã thanh toán")
            {
                TempData["Error"] =
                    "Đơn hàng đã thanh toán. Vui lòng liên hệ bộ phận hỗ trợ để được xử lý hoàn tiền.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            if (order.Status != "Chờ xác nhận" &&
                order.Status != "Đang giao hàng")
            {
                TempData["Error"] =
                    "Đơn hàng hiện tại không thể hủy.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            var storeOrders = await _context.StoreOrders
                .Where(so => so.OrderId == order.Id)
                .ToListAsync();

            // Không cho hủy nếu có cửa hàng đã giao hoặc đã hủy đơn
            if (storeOrders.Any(so =>
                so.Status == "Đã giao hàng" ||
                so.Status == "Đã hủy"))
            {
                TempData["Error"] =
                    "Không thể hủy đơn hàng vì có cửa hàng đã giao hoặc đã hủy đơn.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Hoàn lại tồn kho từng biến thể
                foreach (var detail in order.OrderDetails)
                {
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(v =>
                            v.Id == detail.ProductVariantId);

                    if (variant != null)
                    {
                        variant.Quantity += detail.Quantity;
                    }
                }

                // 2. Cập nhật lại tổng số lượng sản phẩm
                var productIds = order.OrderDetails
                    .Select(d => d.ProductId)
                    .Distinct()
                    .ToList();

                foreach (var productId in productIds)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p =>
                            p.Id == productId);

                    if (product != null)
                    {
                        product.Quantity =
                            await _context.ProductVariants
                                .Where(v => v.ProductId == productId)
                                .SumAsync(v => (int?)v.Quantity) ?? 0;
                    }
                }

                // 3. Hủy đơn hàng tổng
                order.Status = "Đã hủy";
                order.PaymentStatus = "Đã hủy";

                // 4. Hoàn lại lượt sử dụng voucher
                if (!string.IsNullOrWhiteSpace(order.DiscountCode))
                {
                    var usedCoupon = await _context.DiscountCodes
                        .FirstOrDefaultAsync(c =>
                            c.Code == order.DiscountCode);

                    if (usedCoupon != null &&
                        usedCoupon.UsedCount > 0)
                    {
                        usedCoupon.UsedCount--;
                    }

                    var userCoupon = await _context.UserCoupons
                        .FirstOrDefaultAsync(x =>
                            x.UserId == userId &&
                            x.OrderId == order.Id);

                    if (userCoupon != null)
                    {
                        userCoupon.UsedAt = null;
                        userCoupon.OrderId = null;
                    }
                }

                // 5. Hủy tất cả đơn hàng của cửa hàng
                foreach (var storeOrder in storeOrders)
                {
                    storeOrder.Status = "Đã hủy";
                    storeOrder.UpdatedAt = DateTime.Now;
                }

                // 6. Lưu dữ liệu và hoàn tất transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    "Hủy đơn hàng thành công. Số lượng sản phẩm đã được hoàn lại kho.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }
        }
    }
}
