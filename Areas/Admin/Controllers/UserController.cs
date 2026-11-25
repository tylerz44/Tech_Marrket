using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMarket.Models;

namespace TechMarket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    public UserController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, string? role)
    {
        var users = await _userManager.Users.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var rows = new List<UserAdminRow>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "Chưa phân quyền";
            if (!string.IsNullOrWhiteSpace(keyword) &&
                !(user.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                  (user.Email ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))) continue;
            if (!string.IsNullOrWhiteSpace(role) && roleName != role) continue;
            rows.Add(new UserAdminRow { Id = user.Id, FullName = user.FullName, Email = user.Email ?? "", Phone = user.PhoneNumber ?? "", Role = roleName, IsActive = user.IsActive, CreatedAt = user.CreatedAt });
        }
        ViewBag.Keyword = keyword; ViewBag.Role = role;
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        { TempData["Error"] = "Không thể khóa tài khoản quản trị viên tại đây."; return RedirectToAction(nameof(Index)); }
        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? (user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.") : "Không thể cập nhật tài khoản.";
        return RedirectToAction(nameof(Index));
    }


}

public class UserAdminRow
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
