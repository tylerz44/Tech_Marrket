
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Data;
using TechMarket.Models;

namespace TechMarket.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ShippingAddressController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShippingAddressController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        // GET: ShippingAddress/Index
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();

            var addresses = await _context.ShippingAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            return View(addresses);
        }

        // GET: ShippingAddress/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: ShippingAddress/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShippingAddress model)
        {
            var userId = GetUserId();

            // Không cho người dùng tự truyền UserId
            ModelState.Remove(nameof(model.UserId));
            ModelState.Remove(nameof(model.User));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool hasAddress = await _context.ShippingAddresses
                .AnyAsync(a => a.UserId == userId);

            var address = new ShippingAddress
            {
                UserId = userId,
                AddressName = model.AddressName,
                ReceiverName = model.ReceiverName,
                Phone = model.Phone,
                Address = model.Address,
                IsDefault = model.IsDefault || !hasAddress
            };

            if (address.IsDefault)
            {
                var oldDefaults = await _context.ShippingAddresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();

                foreach (var item in oldDefaults)
                {
                    item.IsDefault = false;
                }
            }

            _context.ShippingAddresses.Add(address);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm địa chỉ thành công!";

            return RedirectToAction(nameof(Index));
        }

        // GET: ShippingAddress/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetUserId();

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            return View(address);
        }

        // POST: ShippingAddress/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ShippingAddress model)
        {
            var userId = GetUserId();

            ModelState.Remove(nameof(model.UserId));
            ModelState.Remove(nameof(model.User));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            address.AddressName = model.AddressName;
            address.ReceiverName = model.ReceiverName;
            address.Phone = model.Phone;
            address.Address = model.Address;

            if (model.IsDefault)
            {
                var oldDefaults = await _context.ShippingAddresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();

                foreach (var item in oldDefaults)
                {
                    item.IsDefault = false;
                }

                address.IsDefault = true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật địa chỉ thành công!";

            return RedirectToAction(nameof(Index));
        }

        // POST: ShippingAddress/SetDefault
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = GetUserId();

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            var addresses = await _context.ShippingAddresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var item in addresses)
            {
                item.IsDefault = item.Id == id;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã đặt địa chỉ mặc định!";

            return RedirectToAction(nameof(Index));
        }

        // POST: ShippingAddress/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a =>
                    a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            bool wasDefault = address.IsDefault;

            _context.ShippingAddresses.Remove(address);
            await _context.SaveChangesAsync();

            // Nếu xóa địa chỉ mặc định, chọn địa chỉ khác làm mặc định
            if (wasDefault)
            {
                var nextAddress = await _context.ShippingAddresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefaultAsync();

                if (nextAddress != null)
                {
                    nextAddress.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Đã xóa địa chỉ!";

            return RedirectToAction(nameof(Index));
        }
    }
}
