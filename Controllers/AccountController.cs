
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechMarket.Models;
using TechMarket.ViewModels;

namespace TechMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // =====================================
        // GET: REGISTER
        // =====================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // =====================================
        // POST: REGISTER
        // =====================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Tạo tài khoản mới
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(
                user, model.Password);

            // Kiểm tra tạo tài khoản
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                return View(model);
            }

            // Tự động cấp quyền Customer
            var roleResult = await _userManager.AddToRoleAsync(
                user, "Customer");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty, error.Description);
                }

                await _userManager.DeleteAsync(user);

                return View(model);
            }

            // Đăng nhập sau khi đăng ký
            await _signInManager.SignInAsync(
                user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        // =====================================
        // GET: LOGIN
        // =====================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // =====================================
        // POST: LOGIN
        // =====================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Tìm tài khoản theo Email
            var user = await _userManager.FindByEmailAsync(
                model.Email);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác.");

                return View(model);
            }

            // Kiểm tra mật khẩu
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Nếu có ReturnUrl hợp lệ
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Chuyển hướng Admin
                if (await _userManager.IsInRoleAsync(
                    user, "Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard",
                        new { area = "Admin" });
                }

                // Chuyển hướng StoreOwner
                if (await _userManager.IsInRoleAsync(
                    user, "StoreOwner"))
                {
                    return RedirectToAction(
                        "Index",
                        "Dashboard",
                        new { area = "Store" });
                }

                // Customer về trang chủ
                return RedirectToAction(
                    "Index", "Home");
            }

            ModelState.AddModelError(
                string.Empty,
                "Email hoặc mật khẩu không chính xác.");

            return View(model);
        }

        // =====================================
        // POST: LOGOUT
        // =====================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index", "Home");
        }

        // =====================================
        // GET: ACCESS DENIED
        // =====================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
