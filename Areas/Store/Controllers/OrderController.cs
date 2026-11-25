
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;
using StoreEntity = TechMarket.Models.Store;

namespace TechMarket.Areas.Store.Controllers
{
    [Area("Store")]
    [Authorize(Roles = "StoreOwner")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Các trạng thái được phép cập nhật bằng dropdown
        // Không bao gồm "Đã hủy" vì phải sử dụng chức năng Cancel
        private readonly string[] AllowedStatuses =
        {
            "Chờ xác nhận",
            "Đang giao hàng",
            "Đã giao hàng"
        };

        // Lấy cửa hàng của StoreOwner hiện tại
        private async Task<StoreEntity?> GetCurrentStoreAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _context.Stores
                .FirstOrDefaultAsync(s => s.OwnerId == userId);
        }

        // Đồng bộ trạng thái đơn hàng tổng cho Customer
        private async Task UpdateParentOrderStatusAsync(int orderId)
        {
            var statuses = await _context.StoreOrders
                .Where(so => so.OrderId == orderId)
                .Select(so => so.Status)
                .ToListAsync();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || !statuses.Any())
            {
                return;
            }

            // Tất cả cửa hàng đã giao hàng
            if (statuses.All(s => s == "Đã giao hàng"))
            {
                order.Status = "Đã giao hàng";
            }
            // Tất cả cửa hàng đã hủy
            else if (statuses.All(s => s == "Đã hủy"))
            {
                order.Status = "Đã hủy";
            }
            // Có cửa hàng đang giao hoặc đã giao
            else if (statuses.Any(s =>
                s == "Đang giao hàng" ||
                s == "Đã giao hàng"))
            {
                order.Status = "Đang giao hàng";
            }
            else
            {
                order.Status = "Chờ xác nhận";
            }
        }

        // GET: Store/Order/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var store = await GetCurrentStoreAsync();

            if (store == null)
            {
                return NotFound(
                    "Tài khoản chưa có thông tin cửa hàng.");
            }

            var storeOrders = await _context.StoreOrders
                .Where(so => so.StoreId == store.Id)
                .Include(so => so.Order)
                    .ThenInclude(o => o.OrderDetails)
                .OrderByDescending(so => so.CreatedAt)
                .ToListAsync();

            var orders = storeOrders
                .Select(so => so.Order)
                .ToList();

            foreach (var order in orders)
            {
                order.OrderDetails = order.OrderDetails
                    .Where(d => d.StoreId == store.Id)
                    .ToList();
            }

            ViewBag.StoreOrders = storeOrders.ToDictionary(
                so => so.OrderId);

            return View(orders);
        }

        // GET: Store/Order/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var store = await GetCurrentStoreAsync();

            if (store == null)
            {
                return NotFound(
                    "Tài khoản chưa có thông tin cửa hàng.");
            }

            var storeOrder = await _context.StoreOrders
                .Include(so => so.Order)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(so =>
                    so.OrderId == id &&
                    so.StoreId == store.Id);

            if (storeOrder == null)
            {
                return NotFound();
            }

            var order = storeOrder.Order;

            order.OrderDetails = order.OrderDetails
                .Where(d => d.StoreId == store.Id)
                .ToList();

            ViewBag.StoreOrder = storeOrder;

            return View(order);
        }

        // POST: Store/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status)
        {
            var store = await GetCurrentStoreAsync();

            if (store == null)
            {
                return NotFound(
                    "Tài khoản chưa có thông tin cửa hàng.");
            }

            if (string.IsNullOrWhiteSpace(status) ||
                !AllowedStatuses.Contains(status))
            {
                TempData["Error"] =
                    "Trạng thái không hợp lệ. Hãy sử dụng chức năng hủy đơn nếu muốn hủy.";

                return RedirectToAction(nameof(Index));
            }

            var storeOrder = await _context.StoreOrders
                .FirstOrDefaultAsync(so =>
                    so.Id == id &&
                    so.StoreId == store.Id);

            if (storeOrder == null)
            {
                return NotFound();
            }

            if (storeOrder.Status == "Đã hủy" ||
                storeOrder.Status == "Đã giao hàng")
            {
                TempData["Error"] =
                    "Không thể thay đổi trạng thái đơn hàng này.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = storeOrder.OrderId });
            }

            // Kiểm tra trạng thái thanh toán
            var paymentInfo = await _context.Orders
                .Where(o => o.Id == storeOrder.OrderId)
                .Select(o => new
                {
                    o.PaymentMethod,
                    o.PaymentStatus
                })
                .FirstOrDefaultAsync();

            if (paymentInfo != null &&
                paymentInfo.PaymentMethod != "COD" &&
                paymentInfo.PaymentStatus != "Đã thanh toán" &&
                status != "Chờ xác nhận")
            {
                TempData["Error"] =
                    "Khách hàng chưa thanh toán. Cửa hàng chỉ có thể xử lý giao hàng sau khi đơn hàng được thanh toán.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = storeOrder.OrderId });
            }

            // Cập nhật trạng thái cửa hàng
            storeOrder.Status = status;
            storeOrder.UpdatedAt = DateTime.Now;

            // Đồng bộ trạng thái đơn hàng tổng
            await UpdateParentOrderStatusAsync(
                storeOrder.OrderId);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật trạng thái đơn hàng thành công.";

            return RedirectToAction(
                nameof(Details),
                new { id = storeOrder.OrderId });
        }

        // POST: Store/Order/Cancel/5
        // id là OrderId
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var store = await GetCurrentStoreAsync();

            if (store == null)
            {
                return NotFound(
                    "Tài khoản chưa có thông tin cửa hàng.");
            }

            // Chỉ lấy đơn hàng thuộc cửa hàng hiện tại
            var storeOrder = await _context.StoreOrders
                .Include(so => so.Order)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(so =>
                    so.OrderId == id &&
                    so.StoreId == store.Id);

            if (storeOrder == null)
            {
                return NotFound();
            }

            if (storeOrder.Order.PaymentStatus == "Đã thanh toán")
            {
                TempData["Error"] =
                    "Đơn hàng đã thanh toán. Vui lòng liên hệ quản trị viên để xử lý hoàn tiền trước khi hủy.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Chỉ được hủy khi đang chờ xác nhận hoặc đang giao
            if (storeOrder.Status != "Chờ xác nhận" &&
                storeOrder.Status != "Đang giao hàng")
            {
                TempData["Error"] =
                    "Đơn hàng của cửa hàng hiện tại không thể hủy.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Chỉ lấy chi tiết sản phẩm thuộc cửa hàng hiện tại
                var orderDetails = storeOrder.Order.OrderDetails
                    .Where(d => d.StoreId == store.Id)
                    .ToList();

                // Hoàn lại tồn kho các biến thể sản phẩm
                foreach (var detail in orderDetails)
                {
                    var variant = await _context.ProductVariants
                        .FirstOrDefaultAsync(v =>
                            v.Id == detail.ProductVariantId);

                    if (variant != null)
                    {
                        variant.Quantity += detail.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                // Cập nhật tổng số lượng của từng sản phẩm
                var productIds = orderDetails
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
                                .SumAsync(v => v.Quantity);
                    }
                }

                // Hủy đơn hàng riêng của cửa hàng
                storeOrder.Status = "Đã hủy";
                storeOrder.UpdatedAt = DateTime.Now;

                // Đồng bộ trạng thái đơn hàng tổng
                await UpdateParentOrderStatusAsync(id);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] =
                    "Đã hủy đơn hàng của cửa hàng và hoàn lại số lượng sản phẩm vào kho.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }
    }
}
