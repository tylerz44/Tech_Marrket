
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;

namespace TechMarket.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CartSummaryViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Chỉ hiển thị cho khách hàng đã đăng nhập
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated ||
                !User.IsInRole("Customer"))
            {
                return View(0);
            }

            // Lấy ID tài khoản đang đăng nhập
            var claimsPrincipal = User as ClaimsPrincipal;
            var userId = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return View(0);
            }

            // Tính tổng số lượng sản phẩm trong giỏ
            var totalQuantity = await _context.Carts
                .Where(c => c.UserId == userId)
                .SelectMany(c => c.CartItems)
                .SumAsync(ci => (int?)ci.Quantity) ?? 0;

            return View(totalQuantity);
        }
    }
}

