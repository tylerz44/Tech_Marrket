using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using TechMarket.Data;
using TechMarket.ViewModels;

namespace TechMarket.Controllers
{
    [Authorize(Roles = "Customer")]
    public class DemoPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DemoPaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Pay(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound();

            // Chuyển các mã cũ từ bản thử nghiệm VNPay sang mã Demo.
            bool legacyUpdated = false;
            if (order.PaymentMethod == "VNPAYQR")
            {
                order.PaymentMethod = "DEMOQR";
                legacyUpdated = true;
            }
            else if (order.PaymentMethod == "VNBANK")
            {
                order.PaymentMethod = "DEMOBANK";
                legacyUpdated = true;
            }
            else if (order.PaymentMethod == "INTCARD")
            {
                order.PaymentMethod = "DEMOCARD";
                legacyUpdated = true;
            }

            if (order.PaymentMethod != "COD" &&
                (string.IsNullOrWhiteSpace(order.PaymentStatus) ||
                 order.PaymentStatus == "Chưa thanh toán"))
            {
                order.PaymentStatus = "Chờ thanh toán";
                legacyUpdated = true;
            }

            if (order.PaymentMethod != "COD" &&
                string.IsNullOrWhiteSpace(order.PaymentTransactionRef))
            {
                order.PaymentTransactionRef =
                    $"TM-{order.Id}-{Guid.NewGuid():N}".ToUpperInvariant();
                legacyUpdated = true;
            }

            if (legacyUpdated)
                await _context.SaveChangesAsync();

            if (order.Status == "Đã hủy")
            {
                TempData["Error"] = "Đơn hàng đã bị hủy, không thể thanh toán.";
                return RedirectToAction("Index", "OrderHistory");
            }

            if (order.PaymentStatus == "Đã thanh toán")
            {
                TempData["Success"] = "Đơn hàng này đã được thanh toán.";
                return RedirectToAction("Details", "OrderHistory", new { id });
            }

            if (order.PaymentMethod == "COD")
            {
                TempData["Error"] = "Đơn hàng này sử dụng phương thức COD.";
                return RedirectToAction("Details", "OrderHistory", new { id });
            }

            if (order.PaymentMethod != "DEMOQR" &&
                order.PaymentMethod != "DEMOBANK" &&
                order.PaymentMethod != "DEMOCARD")
            {
                TempData["Error"] =
                    "Phương thức thanh toán của đơn hàng không được hỗ trợ.";
                return RedirectToAction("Details", "OrderHistory", new { id });
            }

            if (order.PaymentStatus != "Chờ thanh toán" &&
                order.PaymentStatus != "Thanh toán thất bại")
            {
                TempData["Error"] = "Trạng thái thanh toán không hợp lệ.";
                return RedirectToAction("Details", "OrderHistory", new { id });
            }

            string qrPayload =
                $"TECHMARKET-DEMO|ORDER={order.Id}|AMOUNT={order.TotalAmount:0.00}|REF={order.PaymentTransactionRef}";

            string qrBase64 = string.Empty;

            if (order.PaymentMethod == "DEMOQR")
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(
                    qrPayload, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);
                qrBase64 = Convert.ToBase64String(qrCode.GetGraphic(8));
            }

            var model = new DemoPaymentViewModel
            {
                OrderId = order.Id,
                ReceiverName = order.ReceiverName,
                PaymentMethod = order.PaymentMethod,
                PaymentMethodName = GetPaymentMethodName(order.PaymentMethod),
                PaymentStatus = order.PaymentStatus,
                TotalAmount = order.TotalAmount,
                TransactionRef = order.PaymentTransactionRef ?? string.Empty,
                QrCodeBase64 = qrBase64,
                CreatedAt = order.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int orderId, bool success)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return NotFound();

            if (order.Status == "Đã hủy")
            {
                TempData["Error"] = "Đơn hàng đã bị hủy.";
                return RedirectToAction("Index", "OrderHistory");
            }

            if (order.PaymentMethod == "COD")
            {
                TempData["Error"] = "Đơn hàng COD không cần thanh toán qua cổng Demo.";
                return RedirectToAction("Details", "OrderHistory", new { id = order.Id });
            }

            if (order.PaymentStatus == "Đã thanh toán")
            {
                TempData["Success"] = "Đơn hàng đã được thanh toán trước đó.";
                return RedirectToAction("Details", "OrderHistory", new { id = order.Id });
            }

            if (order.PaymentStatus != "Chờ thanh toán" &&
                order.PaymentStatus != "Thanh toán thất bại")
            {
                TempData["Error"] = "Trạng thái thanh toán không hợp lệ.";
                return RedirectToAction("Details", "OrderHistory", new { id = order.Id });
            }

            if (success)
            {
                order.PaymentStatus = "Đã thanh toán";
                order.PaymentTransactionNo =
                    "DEMO-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
                order.PaidAt = DateTime.Now;

                TempData["Success"] = "Thanh toán mô phỏng thành công!";
            }
            else
            {
                order.PaymentStatus = "Thanh toán thất bại";
                order.PaymentTransactionNo = null;
                order.PaidAt = null;

                TempData["Error"] =
                    "Giao dịch mô phỏng thất bại. Bạn có thể thử thanh toán lại.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "OrderHistory", new { id = order.Id });
        }

        private static string GetPaymentMethodName(string method)
        {
            return method switch
            {
                "DEMOQR" => "Quét mã QR Demo",
                "DEMOBANK" => "Ngân hàng nội địa Demo",
                "DEMOCARD" => "Thẻ quốc tế Demo",
                _ => method
            };
        }
    }
}
