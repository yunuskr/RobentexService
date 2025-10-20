using Microsoft.AspNetCore.Mvc;
using RobentexService.Data;
using RobentexService.Models;
using Microsoft.EntityFrameworkCore;
using RobentexService.Services.Email;
namespace RobentexService.Controllers;

public class ServiceController(ApplicationDbContext db, ILogger<ServiceController> logger,IEmailSender email)
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
        if (!ModelState.IsValid) return View(model);

        try
        {
            if (string.IsNullOrWhiteSpace(model.RobentexOrderNo))
                model.RobentexOrderNo = await GenerateMonthlyRobentexNoAsync(db);

            model.CreatedAt = DateTime.UtcNow.AddHours(3);
            model.UpdatedAt = model.CreatedAt;

            db.ServiceRequests.Add(model);

            for (int attempt = 0; attempt < 2; attempt++)
            {
                try { await db.SaveChangesAsync(); break; }
                catch (DbUpdateException ex)
                {
                    logger.LogWarning(ex, "RobentexOrderNo çakıştı, tekrar üretiliyor...");
                    model.RobentexOrderNo = await GenerateMonthlyRobentexNoAsync(db);
                }
            }

            // E-posta — hata oluşursa kullanıcı akışını bozma
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var subject = $"Talebiniz alındı • {model.RobentexOrderNo}";
                var html = $@"
<div style=""font-family:Segoe UI,Arial,sans-serif;font-size:14px;"">
  <p>Merhaba {(model.FirstName + " " + model.LastName).Trim()},</p>
  <p>Servis talebiniz başarıyla alındı.</p>
  <ul>
    <li><b>Talep No:</b> {model.RobentexOrderNo}</li>
    <li><b>Firma:</b> {model.CompanyName}</li>
    <li><b>Model/Seri:</b> {model.RobotModel} / {model.RobotSerial}</li>
    <li><b>Oluşturma:</b> {model.CreatedAt:dd.MM.yyyy HH:mm}</li>
  </ul>
  <p><b>Arıza Tanımı:</b><br/>{System.Net.WebUtility.HtmlEncode(model.FaultDescription)}</p>
  <p>İlginiz için teşekkür ederiz.<br/>Robentex Destek</p>
</div>";
                try
                {
                    // İstersen fire-and-forget de yapabilirsin: _ = email.SendAsync(...)
                    await email.SendAsync(model.Email, subject, html);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Onay e-postası gönderilemedi: {Email}", model.Email);
                    // bilinçli olarak yutmamız iyi: kullanıcı akışını bozma
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

