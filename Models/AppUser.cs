using System.ComponentModel.DataAnnotations;

namespace RobentexService.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow.AddHours(3);
    public DateTime? LastLoginUtc { get; set; }
}
