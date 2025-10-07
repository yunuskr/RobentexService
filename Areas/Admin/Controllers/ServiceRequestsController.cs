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

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateDto dto)
{
    // basit zorunlu alan kontrolü
    if (string.IsNullOrWhiteSpace(dto.CompanyName) ||
        string.IsNullOrWhiteSpace(dto.FirstName)   ||
        string.IsNullOrWhiteSpace(dto.LastName)    ||
        string.IsNullOrWhiteSpace(dto.Phone)       ||
        string.IsNullOrWhiteSpace(dto.FaultDescription))
    {
        return BadRequest("Zorunlu alanlar eksik.");
    }

    var entity = new ServiceRequest
    {
        CompanyName = dto.CompanyName?.Trim(),
        Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title!.Trim(),
        FirstName = dto.FirstName?.Trim(),
        LastName  = dto.LastName?.Trim(),
        Phone     = dto.Phone?.Trim(),
        Email     = dto.Email?.Trim(),
        RobotModel  = dto.RobotModel?.Trim(),
        RobotSerial = dto.RobotSerial?.Trim(),
        CustomerOrderNo = dto.CustomerOrderNo?.Trim(),
        RobentexOrderNo = dto.RobentexOrderNo?.Trim(),
        TrackingNo = dto.TrackingNo?.Trim(),
        Status = dto.Status,
        FaultDescription = dto.FaultDescription?.Trim(),
        CreatedAt = DateTime.UtcNow.AddHours(3),
        UpdatedAt = DateTime.UtcNow.AddHours(3)    // sende alanın adı UpdatedAt
    };

    if (!string.IsNullOrWhiteSpace(dto.NewNote))
    {
        entity.Notes = new List<ServiceRequestNote>{
            new ServiceRequestNote{
                Text = dto.NewNote!.Trim(),
                CreatedAt = DateTime.UtcNow.AddHours(3),
                CreatedBy = User.Identity?.Name ?? "admin"
            }
        };
    }

    db.ServiceRequests.Add(entity);
    await db.SaveChangesAsync();
    return Ok(new { id = entity.Id });   // JS tarafı sayfayı yeniliyor
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
        s.UpdatedAt        = DateTime.UtcNow.AddHours(3);

        if (!string.IsNullOrWhiteSpace(NewNote))
        {
            db.ServiceRequestNotes.Add(new ServiceRequestNote{
                ServiceRequestId = s.Id,
                Text      = NewNote.Trim(),
                CreatedBy = User.Identity?.Name ?? "admin",
                CreatedAt = DateTime.UtcNow.AddHours(3)
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
