using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RobentexService.Data;
using RobentexService.Models;
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

    // Kaydet (POST) – admin alanları güncelle + not ekle (varsa)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int Id, string? Title, string? TrackingNo,
        string? CustomerOrderNo, string? RobentexOrderNo, ServiceStatus Status, string? NewNote)
    {
        var s = await db.ServiceRequests.Include(x => x.Notes).FirstOrDefaultAsync(x => x.Id == Id);
        if (s == null) return NotFound();

        s.Title            = Title;
        s.TrackingNo       = TrackingNo;
        s.CustomerOrderNo  = CustomerOrderNo;
        s.RobentexOrderNo  = RobentexOrderNo;
        s.Status           = Status;
        s.UpdatedAt        = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(NewNote))
        {
            db.ServiceRequestNotes.Add(new ServiceRequestNote{
                ServiceRequestId = s.Id,
                Text      = NewNote.Trim(),
                CreatedBy = User.Identity?.Name ?? "admin",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        // AJAX çağrısı olduğu için 200 dönmek yeterli
        return Ok();
    }

    // (opsiyonel) Sil
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var req = await db.ServiceRequests.FindAsync(id);
        if (req == null) return NotFound();

        db.ServiceRequests.Remove(req);
        await db.SaveChangesAsync();
        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }
}
