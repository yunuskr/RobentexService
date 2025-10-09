using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RobentexService.Data;
using RobentexService.Models;
using RobentexService.Models.ViewModels;

namespace RobentexService.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.ServiceRequests
            .AsNoTracking()
            .Where(x => !x.IsDeleted)                 // << sadece silinmemişler
            .Include(x => x.Notes)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var vm = new HomeIndexViewModel
        {
            YeniTalep        = items.Where(x => x.Status == ServiceStatus.YeniTalep).ToList(),
            TeklifIletildi   = items.Where(x => x.Status == ServiceStatus.TeklifIletildi).ToList(),
            ServisAsamasi    = items.Where(x => x.Status == ServiceStatus.ServisAsamasi).ToList(),
            TeklifReddedildi = items.Where(x => x.Status == ServiceStatus.TeklifReddedildi).ToList(),
            Tamamlandi       = items.Where(x => x.Status == ServiceStatus.Tamamlandi).ToList(),
            FaturaEdildi     = items.Where(x => x.Status == ServiceStatus.FaturaEdildi).ToList(),
        };

        return View(vm);
    }
    [HttpGet]
    public async Task<IActionResult> EditData(int id)
    {
        var s = await db.ServiceRequests
            .Include(x => x.Notes)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (s == null) return NotFound();

        var vm = new ServiceRequestAdminEditVM
        {
            Id = s.Id,
            Status = s.Status,
            Title = s.Title,
            TrackingNo = s.TrackingNo,
            CustomerOrderNo = s.CustomerOrderNo,
            RobentexOrderNo = s.RobentexOrderNo,
            CompanyName = s.CompanyName,
            FirstName = s.FirstName,
            LastName = s.LastName,
            Email = s.Email,
            Phone = s.Phone,
            RobotModel = s.RobotModel,
            RobotSerial = s.RobotSerial,
            Notes = s.Notes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => (n.CreatedAt, n.CreatedBy, n.Text ?? string.Empty))
            .ToList()
        };

        return Json(vm); // camelCase / PascalCase farkını JS tarafı tolere ediyor
    }
    [HttpGet]
    public async Task<IActionResult> DetailsData(int id)
    {
        var s = await db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Notes)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (s == null) return NotFound();

        // Sadece silinmemiş notlar
        var activeNotes = (s.Notes ?? []).Where(n => !n.IsDeleted);

        DateTime? lastNote = activeNotes
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => (DateTime?)n.CreatedAt)
            .FirstOrDefault();

        DateTime lastModified = s.UpdatedAt ?? lastNote ?? s.CreatedAt;

        string statusText = s.Status switch
        {
            ServiceStatus.YeniTalep        => "Yeni Talep",
            ServiceStatus.TeklifIletildi   => "Teklif İletildi",
            ServiceStatus.ServisAsamasi    => "Servis Aşaması",
            ServiceStatus.TeklifReddedildi => "Teklif Reddedildi",
            ServiceStatus.Tamamlandi       => "Tamamlandı",
            ServiceStatus.FaturaEdildi     => "Fatura Edildi",
            _ => s.Status.ToString()
        };

        return Json(new {
            id = s.Id,
            companyName   = s.CompanyName,
            title         = s.Title,
            status        = (int)s.Status,
            statusText,
            requesterName = $"{(s.FirstName ?? "").Trim()} {(s.LastName ?? "").Trim()}".Trim(),
            phone         = s.Phone,
            email         = s.Email,
            trackingNo    = s.TrackingNo,
            robotModel    = s.RobotModel,
            robotSerial   = s.RobotSerial,
            lastModifiedUtc = lastModified,
            notes = activeNotes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new {
                    id        = n.Id,          // ★ EKLENDİ
                    createdAt = n.CreatedAt,
                    createdBy = n.CreatedBy,
                    text      = n.Text ?? ""
                })
                .ToList()
        });
    }
        
}
