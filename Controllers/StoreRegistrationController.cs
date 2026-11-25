
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TechMarket.Data;
using TechMarket.Models;
using TechMarket.ViewModels;
using TechMarrket.Models;


namespace TechMarket.Controllers
{
    [Authorize(Roles = "Customer")]
    public class StoreRegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StoreRegistrationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: StoreRegistration/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            var pendingRequest = await _context.StoreRegistrationRequests
                .AnyAsync(x => x.UserId == userId &&
                               x.Status == "Pending");

            if (pendingRequest)
            {
                return RedirectToAction(nameof(Status));
            }

            return View();
        }

        // POST: StoreRegistration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            StoreRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var alreadyPending =
                await _context.StoreRegistrationRequests
                    .AnyAsync(x => x.UserId == userId &&
                                   x.Status == "Pending");

            if (alreadyPending)
            {
                ModelState.AddModelError(
                    "",
                    "Bạn đã có yêu cầu đang chờ duyệt.");

                return View(model);
            }

            var request = new StoreRegistrationRequest
            {
                UserId = userId,
                StoreName = model.StoreName,
                Description = model.Description,
                Address = model.Address,
                Phone = model.Phone,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.StoreRegistrationRequests.Add(request);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Status));
        }

        // GET: StoreRegistration/Status
        [HttpGet]
        public async Task<IActionResult> Status()
        {
            var userId = _userManager.GetUserId(User);

            var requests = await _context.StoreRegistrationRequests
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(requests);
        }
    }
}
