using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RobentexService.Data;
using RobentexService.Models;
using RobentexService.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
namespace RobentexService.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController(ApplicationDbContext db, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]  // <-- ekle
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var username = (vm.Username ?? "").Trim();

        // Kullanıcı adı + aktiflik
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        // Sadece şifre eşit mi?
        if (user == null || user.PasswordHash != vm.Password) // <--- düz karşılaştırma
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            await SaveAudit("LoginFailed", vm.Username, "Plain check: kullanıcı bulunamadı/şifre uyumsuz");
            return View(vm);
        }

        // --- cookie sign-in (aynen bırak) ---
        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("display_name", user.DisplayName ?? user.Username),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = vm.RememberMe,
                ExpiresUtc = vm.RememberMe ? DateTimeOffset.UtcNow.AddDays(14)
                                        : DateTimeOffset.UtcNow.AddHours(12)
            });

        user.LastLoginUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await SaveAudit("LoginSuccess", user.Username, "Plain check: success");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        string? username = User.Identity?.Name;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SaveAudit("Logout", username, null);
        return RedirectToAction("Login");
    }

    private async Task SaveAudit(string action, string? username, string? details)
    {
        try
        {
            var entity = new AuditLog
            {
                Username = username,
                Action = action,
                Details = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            };

            // İmza atmışsa UserId’yi doldur
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid))
                    entity.UserId = uid;
            }

            db.AuditLogs.Add(entity);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Audit kaydı yazılamadı.");
        }
    }
}
