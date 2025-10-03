using System.ComponentModel.DataAnnotations;

namespace RobentexService.Models;

public class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }          // anon da olabilir
    [StringLength(64)]
    public string? Username { get; set; }      // hızlı rapor için denormalize

    [Required, StringLength(128)]
    public string Action { get; set; } = "";   // "LoginSuccess", "Create:ServiceRequest", ...

    [StringLength(2048)]
    public string? Details { get; set; }       // isteğe bağlı JSON / mesaj

    [StringLength(64)]
    public string? IpAddress { get; set; }

    [StringLength(256)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
