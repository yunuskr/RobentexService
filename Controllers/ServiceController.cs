using Microsoft.AspNetCore.Mvc;
using RobentexService.Data;
using RobentexService.Models;
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

    // ——— TR yerel zamanı (cross-platform) ———
    private static DateTime GetTurkeyLocalNow()
    {
        try
        {
            // Windows
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            // Linux / macOS
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
    }

    // ——— YYMM + 2 haneli sıra (01..99) => RXYYMMNN ———
    private static string MakeMonthlyPrefix(DateTime trNow)
    {
        var yy = trNow.ToString("yy"); // 25
        var mm = trNow.ToString("MM"); // 10
        return $"RX{yy}{mm}";          // RX2510
    }

    /// <summary>
    /// Aynı ay (YYMM) içinde 2 haneli sıra numarasıyla benzersiz kod üretir: RXYYMMNN (NN: 01..99).
    /// Yarış durumlarında DbUpdateException olursa tekrar dener.
    /// </summary>
    private static async Task<string> GenerateMonthlyRobentexNoAsync(ApplicationDbContext db, int maxAttempts = 5)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var trNow  = GetTurkeyLocalNow();
            var prefix = MakeMonthlyPrefix(trNow); // RXYYMM

            // Bu ay için mevcut en büyük numarayı bul
            var last = await db.ServiceRequests.AsNoTracking()
                .Where(x => x.RobentexOrderNo != null && x.RobentexOrderNo.StartsWith(prefix))
                .OrderByDescending(x => x.RobentexOrderNo)
                .Select(x => x.RobentexOrderNo!)
                .FirstOrDefaultAsync();

            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 2)
            {
                var tail = last.Substring(prefix.Length, 2); // son 2 hane
                if (int.TryParse(tail, out var n))
                    next = n + 1;
            }

            if (next > 99)
                throw new InvalidOperationException("Bu ay için azami 99 talep numarası aşıldı (RXYYMMNN).");

            var candidate = prefix + next.ToString("00"); // RXYYMMNN

            // Aynı anda başka thread oluşturmuşsa çakışmayı önlemek için var mı kontrolü
            var exists = await db.ServiceRequests.AsNoTracking()
                .AnyAsync(x => x.RobentexOrderNo == candidate);

            if (!exists)
                return candidate;

            // Varsa döngü bir kez daha denesin (yarış ihtimali)
            await Task.Delay(20);
        }

        throw new InvalidOperationException("Benzersiz Robentex sipariş numarası üretilemedi (RXYYMMNN).");
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Index(ServiceRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // RobentexOrderNo boşsa yeni kurala göre oluştur
            if (string.IsNullOrWhiteSpace(model.RobentexOrderNo))
                model.RobentexOrderNo = await GenerateMonthlyRobentexNoAsync(db);

            // Kayıt zamanları (TR saati istiyorsun diye UTC+3 set etmeye devam)
            model.CreatedAt = DateTime.UtcNow.AddHours(3);
            model.UpdatedAt = model.CreatedAt;

            db.ServiceRequests.Add(model);

            // Çok nadir yarışta unique constraint çakışmasını tolere et
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await db.SaveChangesAsync();
                    break; // kaydedildi
                }
                catch (DbUpdateException ex)
                {
                    logger.LogWarning(ex, "RobentexOrderNo çakıştı, tekrar üretiliyor...");
                    // Yeniden üret ve tekrar dene
                    model.RobentexOrderNo = await GenerateMonthlyRobentexNoAsync(db);
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
