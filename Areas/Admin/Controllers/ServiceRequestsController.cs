using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RobentexService.Data;
using RobentexService.Models;
using System.Runtime.InteropServices;
using RobentexService.Models.ViewModels;

namespace RobentexService.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ServiceRequestsController(ApplicationDbContext db, ILogger<ServiceRequestsController> logger) : Controller
{
    // Modal içeriğini dönen GET (Partial)
    [HttpGet]
    public async Task<IActionResult> EditModal(int id)
    {
        var req = await db.ServiceRequests
            .Include(r => r.Notes.OrderByDescending(n => n.CreatedAt))
            .FirstOrDefaultAsync(r => r.Id == id);

        if (req == null) return NotFound();

        var vm = new ServiceRequestAdminEditVM
        {
            Id = req.Id,
            CompanyName = req.CompanyName,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Phone = req.Phone,
            Email = req.Email,
            RobotModel = req.RobotModel,
            RobotSerial = req.RobotSerial,
            Title = req.Title,
            TrackingNo = req.TrackingNo,
            CustomerOrderNo = req.CustomerOrderNo,
            RobentexOrderNo = req.RobentexOrderNo,
            Status = req.Status,
            Notes = req.Notes.Select(n => (n.CreatedAt, n.CreatedBy, n.Text)).ToList()
        };
        Response.Headers["X-Debug-View"] = "_EditRequestModal";
        return PartialView("_EditRequestModal", vm);
    }
    // Basit DTO (form alanları)
    public sealed class CreateDto
    {
        public string? CompanyName { get; set; }
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? RobotModel { get; set; }
        public string? RobotSerial { get; set; }
        public string? CustomerOrderNo { get; set; }
        public string? RobentexOrderNo { get; set; }
        public string? TrackingNo { get; set; }
        public ServiceStatus Status { get; set; } = ServiceStatus.YeniTalep;
        public string? FaultDescription { get; set; }
        public string? NewNote { get; set; }
    }


    // === Yardımcılar (aynısını kullanabiliriz) ===
    private static DateTime GetTurkeyLocalNow()
    {
        try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")); }
        catch { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")); }
    }
    private static string MakeMonthlyPrefix(DateTime trNow)
    {
        var yy = trNow.ToString("yy"); // 25
        var mm = trNow.ToString("MM"); // 10
        return $"RX{yy}{mm}";          // RX2510
    }
    private static async Task<string> GenerateMonthlyRobentexNoAsync(ApplicationDbContext db, int maxAttempts = 5)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var trNow  = GetTurkeyLocalNow();
            var prefix = MakeMonthlyPrefix(trNow); // RXYYMM

            var last = await db.ServiceRequests.AsNoTracking()
                .Where(x => x.RobentexOrderNo != null && x.RobentexOrderNo.StartsWith(prefix))
                .OrderByDescending(x => x.RobentexOrderNo)
                .Select(x => x.RobentexOrderNo!)
                .FirstOrDefaultAsync();

            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 2)
            {
                var tail = last.Substring(prefix.Length, 2);
                if (int.TryParse(tail, out var n)) next = n + 1;
            }
            if (next > 99) throw new InvalidOperationException("Bu ay için 99 üzeri numara.");

            var candidate = prefix + next.ToString("00");

            var exists = await db.ServiceRequests.AsNoTracking()
                .AnyAsync(x => x.RobentexOrderNo == candidate);

            if (!exists) return candidate;

            await Task.Delay(20);
        }
        throw new InvalidOperationException("Benzersiz RX numarası üretilemedi.");
    }

    // === JS’in çağıracağı GET endpoint ===
    [HttpGet]
    public async Task<IActionResult> NextRobentexNo()
    {
        try
        {
            var code = await GenerateMonthlyRobentexNoAsync(db);
            return Json(new { ok = true, value = code });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "NextRobentexNo hata");
            return Json(new { ok = false, message = "Numara üretilemedi." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDto dto)
    {
        // Eksik alanları topla (Müşteri Sipariş No HARİÇ)
        var missing = new List<string>();
        void Need(string? v, string label)
        {
            if (string.IsNullOrWhiteSpace(v)) missing.Add(label);
        }

        Need(dto.CompanyName,      "Firma Adı");
        Need(dto.FirstName,        "Ad");
        Need(dto.LastName,         "Soyad");
        Need(dto.Phone,            "Tel");
        Need(dto.Email,            "E-posta");
        Need(dto.RobotModel,       "Robot Model");
        Need(dto.RobotSerial,      "Robot Seri No");
        Need(dto.FaultDescription, "Arıza Tanımı");
        // İSTEĞE BAĞLI: Need(dto.Title, "Başlık");
        // İSTEĞE BAĞLI: Need(dto.TrackingNo, "Takip No");
        // İSTEĞE BAĞLI: Need(dto.RobentexOrderNo, "Robentex Sipariş No"); // adminde genelde opsiyonel

        if (missing.Count > 0)
        {
            // 400 yerine 200 dön → kullanıcı hata kodu görmez
            return Ok(new
            {
                ok = false,
                message = "Zorunlu alanlar eksik.",
                missing
            });
        }
        
        var entity = new ServiceRequest
        {
            CompanyName      = dto.CompanyName!.Trim(),
            Title            = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title!.Trim(),
            FirstName        = dto.FirstName!.Trim(),
            LastName         = dto.LastName!.Trim(),
            Phone            = dto.Phone!.Trim(),
            Email            = dto.Email!.Trim(),
            RobotModel       = dto.RobotModel!.Trim(),
            RobotSerial      = dto.RobotSerial!.Trim(),
            CustomerOrderNo  = string.IsNullOrWhiteSpace(dto.CustomerOrderNo) ? null : dto.CustomerOrderNo!.Trim(), // OPSİYONEL
            RobentexOrderNo  = string.IsNullOrWhiteSpace(dto.RobentexOrderNo) ? null : dto.RobentexOrderNo!.Trim(),
            TrackingNo       = string.IsNullOrWhiteSpace(dto.TrackingNo) ? null : dto.TrackingNo!.Trim(),
            Status           = dto.Status,
            FaultDescription = dto.FaultDescription!.Trim(),
            CreatedAt        = DateTime.UtcNow.AddHours(3),
            UpdatedAt        = DateTime.UtcNow.AddHours(3)
        };

        if (!string.IsNullOrWhiteSpace(dto.NewNote))
        {
            entity.Notes = new List<ServiceRequestNote>{
                new ServiceRequestNote{
                    Text      = dto.NewNote!.Trim(),
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    CreatedBy = User.Identity?.Name ?? "admin"
                }
            };
        }

        db.ServiceRequests.Add(entity);
        await db.SaveChangesAsync();

        return Ok(new { ok = true, id = entity.Id });
    }
    // Kaydet (POST) – admin alanları güncelle + not ekle (varsa)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int Id, string? Title, string? TrackingNo,
        string? CustomerOrderNo, string? RobentexOrderNo, ServiceStatus Status, string? NewNote,string? FaultDescription)
    {
        var s = await db.ServiceRequests.Include(x => x.Notes).FirstOrDefaultAsync(x => x.Id == Id);
        if (s == null) return NotFound();

        s.Title = Title;
        s.TrackingNo = TrackingNo;
        s.CustomerOrderNo = CustomerOrderNo;
        s.RobentexOrderNo = RobentexOrderNo;
        s.Status = Status;
        s.FaultDescription = FaultDescription?.Trim();
        s.UpdatedAt = DateTime.UtcNow.AddHours(3);

        if (!string.IsNullOrWhiteSpace(NewNote))
        {
            db.ServiceRequestNotes.Add(new ServiceRequestNote
            {
                ServiceRequestId = s.Id,
                Text = NewNote.Trim(),
                CreatedBy = User.Identity?.Name ?? "admin",
                CreatedAt = DateTime.UtcNow.AddHours(3)
            });
        }

        await db.SaveChangesAsync();
        // AJAX çağrısı olduğu için 200 dönmek yeterli
        return Ok();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDelete(int id, string? reason)
    {
        try
        {
            var entity = await db.ServiceRequests
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return NotFound($"#{id} talep bulunamadı.");

            if (entity.IsDeleted)
                return BadRequest($"#{id} talep zaten silinmiş durumda.");

            entity.IsDeleted    = true;
            entity.DeletedAt    = DateTime.UtcNow.AddHours(3);
            entity.DeletedBy    = User.Identity?.Name ?? "admin";
            entity.DeleteReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            entity.UpdatedAt    = DateTime.UtcNow.AddHours(3);

            // (Opsiyonel) “silindi” notu
            db.ServiceRequestNotes.Add(new ServiceRequestNote{
                ServiceRequestId = entity.Id,
                Text      = "Talep silindi" + (string.IsNullOrWhiteSpace(reason) ? "" : $" (Neden: {reason})"),
                CreatedAt = DateTime.UtcNow.AddHours(3),
                CreatedBy = User.Identity?.Name ?? "admin"
            });

            await db.SaveChangesAsync();

            // Edit modal genelde AJAX ile çağırıyor; 200 dönmek yeterli
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SoftDelete sırasında hata. Id={Id}", id);
            return StatusCode(500, "Talep silinirken bir hata oluştu.");
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDeleteNote(int noteId, string? reason)
    {
        var note = await db.ServiceRequestNotes.FirstOrDefaultAsync(n => n.Id == noteId);
        if (note == null) return NotFound($"Not #{noteId} bulunamadı.");
        if (note.IsDeleted) return BadRequest("Not zaten silinmiş.");

        note.IsDeleted    = true;
        note.DeletedAt    = DateTime.UtcNow.AddHours(3);
        note.DeletedBy    = User.Identity?.Name ?? "admin";
        note.DeleteReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        await db.SaveChangesAsync();
        return Ok();
    }
}
