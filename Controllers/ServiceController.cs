using Microsoft.AspNetCore.Mvc;
using RobentexService.Data;
using RobentexService.Models;
using Microsoft.EntityFrameworkCore;
using RobentexService.Services.Email;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace RobentexService.Controllers;

public class ServiceController(
    ApplicationDbContext db,
    ILogger<ServiceController> logger,
    IEmailSender email,
    IOptions<ReCaptchaSettings> reCaptchaOptions)
    : Controller
{
    private readonly ReCaptchaSettings _reCaptchaSettings = reCaptchaOptions.Value;

    // 🔒 Basit IP rate limit (in-memory, process ayakta kaldığı sürece)
    private static readonly Dictionary<string, List<DateTime>> _ipRequests = new();
    private static readonly object _rateLock = new();

    private bool IsIpAllowed(int limitPerMinute = 5)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        lock (_rateLock)
        {
            if (!_ipRequests.TryGetValue(ip, out var list))
            {
                list = new List<DateTime>();
                _ipRequests[ip] = list;
            }

            // 1 dakikadan eski kayıtları temizle
            list.RemoveAll(t => (now - t) > TimeSpan.FromMinutes(1));

            if (list.Count >= limitPerMinute)
                return false;

            list.Add(now);
            return true;
        }
    }

    private async Task<bool> IsReCaptchaValid(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        using var client = new HttpClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "secret", _reCaptchaSettings.SecretKey },
            { "response", token }
        });

        var response = await client.PostAsync(
            "https://www.google.com/recaptcha/api/siteverify",
            content);

        if (!response.IsSuccessStatusCode)
            return false;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("success", out var successProp))
            return successProp.GetBoolean();

        return false;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.ReCaptchaSiteKey = _reCaptchaSettings.SiteKey;
        return View(new ServiceRequest());
    }

    // ——— TR yerel zamanı (cross-platform) ———
    private static DateTime GetTurkeyLocalNow()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
    }

    // ——— YYMM + 2 haneli sıra (01..99) => RXYYMMNN ———
    private static string MakeMonthlyPrefix(DateTime trNow)
    {
        var yy = trNow.ToString("yy");
        var mm = trNow.ToString("MM");
        return $"RX{yy}{mm}";
    }

    /// <summary>
    /// Aynı ay (YYMM) içinde 2 haneli sıra numarasıyla benzersiz kod üretir: RXYYMMNN (NN: 01..99).
    /// Yarış durumlarında DbUpdateException olursa tekrar dener.
    /// </summary>
    private static async Task<string> GenerateMonthlyRobentexNoAsync(ApplicationDbContext db, int maxAttempts = 5)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var trNow = GetTurkeyLocalNow();
            var prefix = MakeMonthlyPrefix(trNow);

            var last = await db.ServiceRequests.AsNoTracking()
                .Where(x => x.RobentexOrderNo != null && x.RobentexOrderNo.StartsWith(prefix))
                .OrderByDescending(x => x.RobentexOrderNo)
                .Select(x => x.RobentexOrderNo!)
                .FirstOrDefaultAsync();

            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 2)
            {
                var tail = last.Substring(prefix.Length, 2);
                if (int.TryParse(tail, out var n))
                    next = n + 1;
            }

            if (next > 99)
                throw new InvalidOperationException("Bu ay için azami 99 talep numarası aşıldı (RXYYMMNN).");

            var candidate = prefix + next.ToString("00");

            var exists = await db.ServiceRequests.AsNoTracking()
                .AnyAsync(x => x.RobentexOrderNo == candidate);

            if (!exists)
                return candidate;

            await Task.Delay(20);
        }

        throw new InvalidOperationException("Benzersiz Robentex sipariş numarası üretilemedi (RXYYMMNN).");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ServiceRequest model)
    {
        ViewBag.ReCaptchaSiteKey = _reCaptchaSettings.SiteKey;

        // 1) IP bazlı rate limit kontrolü
        if (!IsIpAllowed())
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogWarning("IP rate limit aşıldı. IP: {Ip}", ip);

            ModelState.AddModelError("", "Çok sık form gönderimi tespit edildi. Lütfen birkaç dakika sonra tekrar deneyin.");
            return View(model);
        }

        // 2) Honeypot kontrolü
        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            logger.LogInformation("Honeypot dolu geldi, muhtemel bot. IP: {Ip}, Website: {Website}", ip, model.Website);

            TempData["ok"] = "Talebiniz alındı. Teşekkürler.";
            return RedirectToAction(nameof(Success));
        }

        // 3) reCAPTCHA kontrolü
        var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
        var isCaptchaValid = await IsReCaptchaValid(recaptchaToken);

        if (!isCaptchaValid)
        {
            logger.LogWarning("reCAPTCHA doğrulaması başarısız. IP: {Ip}",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ModelState.AddModelError("", "Lütfen 'Ben robot değilim' doğrulamasını tamamlayın.");
            return View(model);
        }

        // 4) Basit spam içerik filtresi
        if (!string.IsNullOrWhiteSpace(model.FaultDescription))
        {
            var text = model.FaultDescription.ToLowerInvariant();

            if (text.Contains("http://") || text.Contains("https://") || text.Contains("www."))
            {
                logger.LogWarning("Link içeren içerik spam olarak engellendi. IP: {Ip}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["ok"] = "Talebiniz alındı. Teşekkürler.";
                return RedirectToAction(nameof(Success));
            }
        }

        // 5) Normal model validasyonu
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (string.IsNullOrWhiteSpace(model.RobentexOrderNo))
                model.RobentexOrderNo = await GenerateMonthlyRobentexNoAsync(db);

            var nowTr = GetTurkeyLocalNow();
            model.CreatedAt = nowTr;
            model.UpdatedAt = nowTr;

            db.ServiceRequests.Add(model);

            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await db.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException ex)
                {
                    logger.LogWarning(ex, "RobentexOrderNo çakıştı, tekrar üretiliyor...");
                    model.RobentexOrderNo = await GenerateMonthlyRobentexNoAsync(db);
                }
            }

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
                    await email.SendAsync(model.Email, subject, html);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Onay e-postası gönderilemedi: {Email}", model.Email);
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