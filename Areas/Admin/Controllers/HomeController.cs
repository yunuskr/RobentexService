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
        var q = db.ServiceRequests.AsNoTracking().OrderByDescending(x => x.CreatedAt);

        var vm = new HomeIndexViewModel
        {
            YeniTalep = await q.Where(x => x.Status == ServiceStatus.YeniTalep).ToListAsync(),
            TeklifIletildi = await q.Where(x => x.Status == ServiceStatus.TeklifIletildi).ToListAsync(),
            ServisAsamasi = await q.Where(x => x.Status == ServiceStatus.ServisAsamasi).ToListAsync(),
            TeklifReddedildi = await q.Where(x => x.Status == ServiceStatus.TeklifReddedildi).ToListAsync(),
            Tamamlandi = await q.Where(x => x.Status == ServiceStatus.Tamamlandi).ToListAsync(),
            FaturaEdildi = await q.Where(x => x.Status == ServiceStatus.FaturaEdildi).ToListAsync(),
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

        var vm = new ServiceRequestAdminEditVM {
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
    
}
