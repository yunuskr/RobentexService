using Microsoft.AspNetCore.Mvc;
using RobentexService.Data;
using RobentexService.Models;

namespace RobentexService.Controllers;

public class ServiceController(ApplicationDbContext db, ILogger<ServiceController> logger)
    : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new ServiceRequest());
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Index(ServiceRequest model)
    {
        if (!ModelState.IsValid)
        {
            // doğrulama hataları formda gösterilecek
            return View(model);
        }

        try
        {
            model.CreatedAt = DateTime.UtcNow;
            db.ServiceRequests.Add(model);
            await db.SaveChangesAsync();

            TempData["ok"] = "Talebiniz alındı. Teşekkürler.";
            return RedirectToAction(nameof(Success));
        }
        catch (Exception ex)
        {
            // basit log ve kullanıcıya mesaj
            logger.LogError(ex, "Kayıt hatası");
            ModelState.AddModelError("", "Kayıt sırasında bir hata oluştu.");
            return View(model);
        }
    }

    public IActionResult Success() => View();
}
