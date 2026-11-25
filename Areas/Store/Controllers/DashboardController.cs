
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Areas.Store.Controllers
{
    [Area("Store")]
    [Authorize(Roles = "StoreOwner")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var store = await _context.Stores
                .FirstOrDefaultAsync(x => x.OwnerId == userId);

            if (store == null)
            {
                return NotFound("Tài khoản chưa có thông tin cửa hàng.");
            }

            return View(store);
        }
    }
}
