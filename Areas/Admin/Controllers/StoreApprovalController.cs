using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;
using StoreModel = TechMarket.Models.Store;

namespace TechMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StoreApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StoreApprovalController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Danh sách yêu cầu mở cửa hàng
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.StoreRegistrationRequests
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // Admin duyệt yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.StoreRegistrationRequests
                .FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status != "Pending")
            {
                TempData["Error"] = "Yêu cầu này đã được xử lý.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Không cho một tài khoản tạo nhiều cửa hàng
            var hasStore = await _context.Stores
                .AnyAsync(x => x.OwnerId == user.Id);

            if (hasStore)
            {
                TempData["Error"] = "Tài khoản này đã có cửa hàng.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Chuyển Customer sang StoreOwner
                if (await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    var removeResult =
                        await _userManager.RemoveFromRoleAsync(user, "Customer");

                    if (!removeResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Không thể cập nhật quyền khách hàng.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (!await _userManager.IsInRoleAsync(user, "StoreOwner"))
                {
                    var addResult =
                        await _userManager.AddToRoleAsync(user, "StoreOwner");

                    if (!addResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Không thể cấp quyền StoreOwner.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Tạo cửa hàng
                var store = new StoreModel
                {
                    OwnerId = user.Id,
                    StoreName = request.StoreName,
                    Description = request.Description,
                    Address = request.Address,
                    Phone = request.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Stores.Add(store);

                // Cập nhật trạng thái yêu cầu
                request.Status = "Approved";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Đã duyệt yêu cầu và tạo cửa hàng thành công.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi duyệt yêu cầu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Admin từ chối yêu cầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.StoreRegistrationRequests
                .FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            if (request.Status != "Pending")
            {
                TempData["Error"] = "Yêu cầu này đã được xử lý.";
                return RedirectToAction(nameof(Index));
            }

            request.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã từ chối yêu cầu mở cửa hàng.";

            return RedirectToAction(nameof(Index));
        }
    }
}