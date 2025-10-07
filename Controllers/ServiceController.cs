using Microsoft.AspNetCore.Mvc;
using RobentexService.Data;
using RobentexService.Models;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
namespace RobentexService.Controllers;

public class ServiceController(ApplicationDbContext db, ILogger<ServiceController> logger)
    : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new ServiceRequest());
    }
    private static string MakeRxCode()
    {
        Span<byte> b = stackalloc byte[5];
        RandomNumberGenerator.Fill(b);

        // 5 rakam: ilk rakam 1–9, kalan 0–9
        var digits = new char[5];
        digits[0] = (char)('1' + (b[0] % 9));
        for (int i = 1; i < 5; i++)
            digits[i] = (char)('0' + (b[i] % 10));

        return "RX" + new string(digits); // toplam 7 char
    }

    private static async Task<string> GenerateUniqueRobentexNoAsync(ApplicationDbContext db, int maxAttempts = 10)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            var code = MakeRxCode();
            var exists = await db.ServiceRequests.AsNoTracking()
                            .AnyAsync(x => x.RobentexOrderNo == code);
            if (!exists) return code;
        }
        throw new InvalidOperationException("Benzersiz Robentex sipariş numarası üretilemedi.");
    }
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Index(ServiceRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // RobentexOrderNo boşsa otomatik üret
            if (string.IsNullOrWhiteSpace(model.RobentexOrderNo))
                model.RobentexOrderNo = await GenerateUniqueRobentexNoAsync(db);

            model.CreatedAt = DateTime.UtcNow.AddHours(3);   // TR saati istiyorsan: DateTime.UtcNow.AddHours(3) veya TimeZoneInfo kullan
            model.UpdatedAt = model.CreatedAt;

            db.ServiceRequests.Add(model);

            // Olası unique-constraint çakışmasına karşı 1 kez daha dene
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await db.SaveChangesAsync();
                    break; // kaydedildi
                }
                catch (DbUpdateException)
                {
                    // Çok nadir çakışma: kodu yeniden üret ve bir kez daha dene
                    model.RobentexOrderNo = await GenerateUniqueRobentexNoAsync(db);
                }
            }

            TempData["ok"] = "Talebiniz alındı. Teşekkürler.";
            return RedirectToAction(nameof(Success));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kayıt hatası");
            ModelState.AddModelError("", "Kayıt sırasında bir hata oluştu.");
            return View(model);
        }
    }

    public IActionResult Success() => View();
}
